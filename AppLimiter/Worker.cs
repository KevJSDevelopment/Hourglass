using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using AppLimiterLibrary;
using LimiterMessaging;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;

namespace AppLimiter
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly string _connectionString;
        private readonly Dictionary<string, TimeSpan> _appUsage = new Dictionary<string, TimeSpan>();
        private readonly Dictionary<string, TimeSpan> _appLimits = new Dictionary<string, TimeSpan>();
        private readonly Dictionary<string, string> _processToExecutableMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _shownWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private FileSystemWatcher _watcher;
        private string _configPath;

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AppLimiter", "ProcessLimits.json");
            InitializeFileWatcher();
            InitializeProcessMap();
        }

        private void InitializeProcessMap()
        {
            // Add known mappings here
            _processToExecutableMap["LeagueClient"] = @"C:\Riot Games\League of Legends\LeagueClient.exe";
            _processToExecutableMap["RiotClientServices"] = @"C:\Riot Games\Riot Client\RiotClientServices.exe";
            // Add more mappings as needed
        }

        private void InitializeFileWatcher()
        {
            var directory = Path.GetDirectoryName(_configPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _watcher = new FileSystemWatcher(directory)
            {
                Filter = Path.GetFileName(_configPath),
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnConfigFileChanged;
        }

        public SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LoadAndApplyLimits(); // Initial load
            using (var connection = GetConnection())
            {
                try
                {
                    await connection.OpenAsync(stoppingToken);
                    _logger.LogInformation("Database connection successful");
                    // Perform your database operations here
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error connecting to the database");
                }
            }

            _ = ListenForMessages(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                TrackAppUsage();
                EnforceUsageLimits();
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ListenForMessages(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var server = new NamedPipeServerStream("AppLimiterPipe", PipeDirection.In))
                {
                    await server.WaitForConnectionAsync(stoppingToken);

                    using (var reader = new StreamReader(server))
                    {
                        string message = await reader.ReadLineAsync();
                        if (message.StartsWith("IGNORE:"))
                        {
                            string processName = message.Substring(7);
                            LimitUpdateHandler.IgnoreLimitForDay(processName);
                            _logger.LogInformation($"Ignoring limits for {processName} until tomorrow.");
                        }
                    }
                }
            }
        }

        private void OnConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            _logger.LogInformation("Configuration file changed. Reloading limits.");
            LoadAndApplyLimits();
        }

        private void LoadAndApplyLimits()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    var limits = JsonSerializer.Deserialize<List<ProcessInfo>>(json);

                    _appLimits.Clear();
                    foreach (var limit in limits)
                    {
                        if (TimeSpan.TryParse(limit.KillTime, out TimeSpan killTime) && killTime > TimeSpan.Zero)
                        {
                            _appLimits[limit.Executable.ToLower()] = killTime;
                        }
                        if (TimeSpan.TryParse(limit.WarningTime, out TimeSpan warningTime) && warningTime > TimeSpan.Zero)
                        {
                            _appLimits[limit.Executable.ToLower() + "warning"] = warningTime;
                        }
                        // Add to process map if not already present
                        string processName = Path.GetFileNameWithoutExtension(limit.Executable);
                        if (!_processToExecutableMap.ContainsKey(processName))
                        {
                            _processToExecutableMap[processName] = limit.Executable;
                        }
                    }
                    _logger.LogInformation("Limits updated successfully.");
                }
                else
                {
                    _logger.LogWarning("Configuration file not found. Using empty limits.");
                    _appLimits.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading process limits. Using empty limits.");
                _appLimits.Clear();
            }
        }

        private void SetDefaultLimits()
        {
            _appLimits.Clear();
        }

        private void TrackAppUsage()
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                if (_processToExecutableMap.TryGetValue(process.ProcessName, out string executable))
                {
                    var matchingLimit = _appLimits.Keys.FirstOrDefault(k => k.Equals(executable.ToLower(), StringComparison.OrdinalIgnoreCase));
                    if (matchingLimit != null)
                    {
                        if (!_appUsage.ContainsKey(matchingLimit))
                        {
                            _appUsage[matchingLimit] = TimeSpan.Zero;
                            _appUsage[matchingLimit + "warning"] = TimeSpan.Zero;
                        }
                        _appUsage[matchingLimit] += TimeSpan.FromSeconds(1);
                        _appUsage[matchingLimit + "warning"] += TimeSpan.FromSeconds(1);
                    }
                }
            }
        }

        private void EnforceUsageLimits()
        {
            foreach (var app in _appUsage.Keys.ToList())
            {
                if(LimitUpdateHandler.IsLimitIgnored(app))
                {
                    continue;
                }

                if (_appLimits.TryGetValue(app, out TimeSpan limit) && _appUsage[app] >= limit)
                {
                    if (app.EndsWith("warning"))
                    {
                        string baseApp = app.Substring(0, app.Length - 7); // Remove "warning" suffix
                        if (!_shownWarnings.Contains(baseApp))
                        {
                            ShowWarningMessage(baseApp);
                            _shownWarnings.Add(baseApp);
                        }
                    }
                    else
                    {
                        var processName = _processToExecutableMap
                            .FirstOrDefault(x => x.Value.Equals(app, StringComparison.OrdinalIgnoreCase))
                            .Key;

                        if (!string.IsNullOrEmpty(processName))
                        {
                            var processes = Process.GetProcessesByName(processName);
                            foreach (var process in processes)
                            {
                                try
                                {
                                    process.Kill();
                                    _logger.LogInformation($"Terminated {process.ProcessName} due to exceeded usage limit.");
                                    _shownWarnings.Remove(app); // Reset warning status after termination
                                    _appUsage[app] = TimeSpan.Zero;
                                    _appUsage[app+"warning"] = TimeSpan.Zero;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, $"Failed to terminate {process.ProcessName}.");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"No process name found for executable: {app}");
                        }
                    }
                }
            }
        }

        private void ShowWarningMessage(string executablePath)
        {
            var timeRemaining = _appLimits[executablePath] - _appLimits[executablePath + "warning"];
            var appName = Path.GetFileNameWithoutExtension(executablePath);
            string warningMessage = timeRemaining >= TimeSpan.FromMinutes(1)
                ? $"WARNING: You have been using {appName} for an extended period. The application will close in {timeRemaining.Minutes} minutes if usage continues."
                : $"WARNING: You have been using {appName} for an extended period. The application will close in {timeRemaining.Seconds} seconds if usage continues.";

            _logger.LogWarning(warningMessage);

            try
            {
                // Run the form on a separate thread to avoid blocking the worker
                Task.Run(() =>
                {
                    Application.SetHighDpiMode(HighDpiMode.SystemAware);
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    using (var form = new LimiterMessagingForm(warningMessage, appName))
                    {
                        Application.Run(form);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show LimiterMessaging form.");
            }
        }

        private void ResetDailyUsage()
        {
            _appUsage.Clear();
            _shownWarnings.Clear();
        }
    }
}
