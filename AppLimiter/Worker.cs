namespace AppLimiter
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly int _interval = 5000;
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }
        //https://jsonplaceholder.typicode.com/users/1/todos
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service started at: {time}", DateTimeOffset.Now);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Contacting API, Requesting Data");




                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
