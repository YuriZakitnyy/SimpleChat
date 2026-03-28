using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using ChatServer.Models;
using FirebaseAdmin.Messaging;

namespace ChatServer.Services
{
    public class DeviceTokenStore
    {
        private readonly ConcurrentDictionary<string, DeviceRegistration> _userDevices = new();
        private readonly ILogger<DeviceTokenStore> _logger;
        private readonly string _storageFilePath;
        private readonly object _fileLock = new();

        public DeviceTokenStore(ILogger<DeviceTokenStore> logger, IConfiguration configuration)
        {
            _logger = logger;
            _storageFilePath = configuration["DeviceStorage:FilePath"] ?? "/var/data/devices.json";
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_storageFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation($"Created device storage directory: {directory}");
                    Console.WriteLine($"Created device storage directory: {directory}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create device storage directory: {directory}: {ex}");
                }
            }
            else
            {
                Console.WriteLine($"Using device storage directory: {directory}");
            }
            Load();
        }

        public void RegisterDevice(string deviceId, string deviceToken, string connectionId, string platform = "Android")
        {
            if (deviceToken != null)
            {
                var registration = new DeviceRegistration
                {
                    DeviceId = deviceId,
                    DeviceToken = deviceToken,
                    Platform = platform,
                    RegisteredAt = DateTime.UtcNow,
                    LastActive = DateTime.UtcNow,
                    ConnectionId = connectionId
                };

                _userDevices[deviceId] = registration;
                Console.WriteLine($"Registered device {deviceId} token {deviceToken} connection {connectionId}");
                
                Save();
            }
        }

        public void UnregisterDevice(string deviceId, string deviceToken)
        {
            _userDevices.Remove(deviceId, out var registration);
            _logger.LogInformation($"Unregistered device {deviceId}");
            
            // Save to file after unregistration
            Save();
        }

        public List<string> GetDeviceTokens(string userName)
        {
            return _userDevices.Values
                .Where(d => d.UserName == userName)
                .Select(d => d.DeviceToken)
                .ToList();
        }

        public List<string> GetAllDeviceTokens()
        {
            return _userDevices.Values
                .Select(d => d.DeviceToken)
                .Distinct()
                .ToList();
        }

        public List<DeviceRegistration> GetAllDevices()
        {
            return _userDevices.Values.ToList();
        }

        public void UpdateLastActive(string deviceId)
        {
            if (_userDevices.TryGetValue(deviceId, out var device))
            {
                device.LastActive = DateTime.UtcNow;
                // Optionally save after update (might be too frequent)
                // Save();
            }
        }

        public void CleanupInactiveDevices(TimeSpan inactiveThreshold)
        {
            var cutoffTime = DateTime.UtcNow - inactiveThreshold;
            var old = _userDevices.Where(it => it.Value.LastActive < cutoffTime).ToArray();
            
            foreach (var kvp in old)
            {
                _userDevices.Remove(kvp.Key, out var _);
            }

            if (old.Length > 0)
            {
                Console.WriteLine($"Cleaned up {old.Length} inactive devices");
                Save();
            }
        }

        public DeviceRegistration? FindByConnectionId(string connectionId)
        {
            return _userDevices.Values.FirstOrDefault(it => it.ConnectionId == connectionId);
        }

        public DeviceRegistration? FindByDeviceToken(string deviceToken)
        {
            return _userDevices.Values.FirstOrDefault(it => it.DeviceToken == deviceToken);
        }

        public void UpdateConnectionId(string deviceId, string? connectionId)
        {
            if (_userDevices.TryGetValue(deviceId, out var device))
            {
                device.ConnectionId = connectionId;
                device.LastActive = DateTime.UtcNow;
                _logger.LogInformation($"Updated connection ID for device {deviceId}");
                Save();
            }
        }

        internal void Load()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(_storageFilePath))
                    {
                        Console.WriteLine($"Loading devices from {_storageFilePath}");
                        
                        var json = File.ReadAllText(_storageFilePath);
                        var devices = JsonSerializer.Deserialize<Dictionary<string, DeviceRegistration>>(json);
                        
                        if (devices != null)
                        {
                            _userDevices.Clear();
                            foreach (var kvp in devices)
                            {
                                _userDevices[kvp.Key] = kvp.Value;
                            }
                            
                            Console.WriteLine($"Loaded {_userDevices.Count} devices from storage");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Device storage file not found: {_storageFilePath}.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load devices from {_storageFilePath}: {ex}");
                }
            }
        }

        public void Save()
        {
            lock (_fileLock)
            {
                try
                {
                    var devices = _userDevices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                    
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    
                    var json = JsonSerializer.Serialize(devices, options);
                    
                    // Write to temporary file first, then rename for atomic operation
                    var tempFile = _storageFilePath + ".tmp";
                    File.WriteAllText(tempFile, json);
                    File.Move(tempFile, _storageFilePath, overwrite: true);
                    
                    Console.WriteLine($"Saved {devices.Count} devices to {_storageFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to save devices to {_storageFilePath}: {ex}");
                }
            }
        }

        internal void AssignUser(string user, string connectionId)
        {
            var device = FindByConnectionId(connectionId);
            if (device != null)
            {
                Console.WriteLine($"Assigned user {user} to device {device.DeviceId} for connection {connectionId}");
                device.UserName = user;
                Save();
            }
        }
    }
}