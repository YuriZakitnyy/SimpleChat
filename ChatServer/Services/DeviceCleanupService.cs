namespace ChatServer.Services
{
    public class DeviceCleanupService : BackgroundService
    {
        private readonly DeviceTokenStore _deviceTokenStore;
        private readonly ILogger<DeviceCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
        private readonly TimeSpan _inactiveThreshold = TimeSpan.FromDays(30);

        public DeviceCleanupService(
            DeviceTokenStore deviceTokenStore,
            ILogger<DeviceCleanupService> logger)
        {
            _deviceTokenStore = deviceTokenStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Device Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    _deviceTokenStore.CleanupInactiveDevices(_inactiveThreshold);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in device cleanup service");
                }
            }

            _logger.LogInformation("Device Cleanup Service stopped");
        }
    }
}