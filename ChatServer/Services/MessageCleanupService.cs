namespace ChatServer.Services
{
    public class MessageCleanupService : BackgroundService
    {
        private readonly MessageStore _messageStore;
        private readonly ILogger<MessageCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);
        private readonly int _daysToKeep = 90;

        public MessageCleanupService(
            MessageStore messageStore,
            ILogger<MessageCleanupService> logger)
        {
            _messageStore = messageStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Message Cleanup Service started");

            // Wait 1 hour before first cleanup
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running message cleanup...");
                    _messageStore.DeleteOldFiles(_daysToKeep);
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in message cleanup service");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }

            _logger.LogInformation("Message Cleanup Service stopped");
        }
    }
}