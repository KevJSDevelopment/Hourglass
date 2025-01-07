using AppLimiterLibrary.Data;
using AppLimiterLibrary.Dtos;
using LimiterMessaging.WPF.Converters;
using LimiterMessaging.WPF.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
namespace LimiterMessaging.WPF.Services
{
    public class WarningWindowManager
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private Application _app;
        private Thread _uiThread;
        private TaskCompletionSource<bool> _appReadyTcs;

        private Application CreateApplication()
        {
            var app = new Application();
            var resources = new ResourceDictionary();

            resources.Add("BoolToVis", new BooleanToVisibilityConverter());
            resources.Add("AccentColor", (Color)ColorConverter.ConvertFromString("#FFA8C69F"));
            resources.Add("AccentBrush", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA8C69F")));
            resources.Add("PlayPauseConverter", new PlayPauseConverter());

            app.Resources = resources;
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;  // Important!
            return app;
        }

        private void EnsureUIThread()
        {
            if (_app == null)
            {
                _appReadyTcs = new TaskCompletionSource<bool>();
                _uiThread = new Thread(() =>
                {
                    _app = CreateApplication();
                    _appReadyTcs.SetResult(true);
                    Dispatcher.Run();
                });
                _uiThread.SetApartmentState(ApartmentState.STA);
                _uiThread.Start();
                _appReadyTcs.Task.Wait();
            }
        }

        public async Task ShowWarning(
            MotivationalMessage message,
            string warning,
            string processName,
            string computerId,
            Action<string, bool> updateIgnoreStatus,
            AppRepository appRepo,
            MotivationalMessageRepository messageRepo,
            SettingsRepository settingsRepo,
            List<MotivationalMessage> messagesSent)
        {
            await _semaphore.WaitAsync();
            try
            {
                EnsureUIThread();

                var tcs = new TaskCompletionSource<bool>();

                await _app.Dispatcher.InvokeAsync(() =>
                {
                    var window = new MessagingWindow(
                        message,
                        warning,
                        processName,
                        computerId,
                        updateIgnoreStatus,
                        appRepo,
                        messageRepo,
                        settingsRepo,
                        messagesSent);

                    window.Closed += (s, e) => tcs.SetResult(true);
                    window.Show();
                });

                await tcs.Task;
            }
            catch (Exception ex)
            {
                updateIgnoreStatus(processName, false);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Cleanup()
        {
            if (_app != null)
            {
                _app.Dispatcher.InvokeAsync(() =>
                {
                    _app.Shutdown();
                });
                _app = null;
                _uiThread = null;
            }
        }
    }
}
