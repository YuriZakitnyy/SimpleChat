using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChatCommon;
using Microsoft.AspNetCore.SignalR.Client;

namespace ChatClientCommon
{
    public class HubClient : IDisposable
    {
        
        private HubConnection _connection;
        public Action<ChatMessage> ReceivedMessage;
        public Action Reconnecting;
        public Action Reconnected;
        public Action ConnectionClosed;

        public HubClient()
        {
        }

        public bool IsConnected => _connection?.State == HubConnectionState.Connected;

        public async Task ConnectAsync(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithUrl(hubUrl, options =>
                {
                    options.TransportMaxBufferSize = CommonConstants.MaxMessageBytes;
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<ChatMessage>("ReceiveMessage", (message) =>
            {
                ReceivedMessage(message);
                return Task.CompletedTask;
            });

            _connection.Reconnecting += error =>
            {
                Reconnecting();
                return Task.CompletedTask;
            };

            _connection.Reconnected += connectionId =>
            {
                Reconnected();
                return Task.CompletedTask;
            };

            _connection.Closed += async error =>
            {
                ConnectionClosed();
                await Task.CompletedTask;
            };

            await _connection.StartAsync().ConfigureAwait(false);
        }

        public Task SendMessageAsync(ChatMessage message)
        {
            return _connection.InvokeAsync("SendMessage", message);
        }

        public Task<IEnumerable<ChatMessage>> ListMessagesAsync(DateTime date, string userName)
        {
            return _connection.InvokeAsync<IEnumerable<ChatMessage>>("ListMessages", date, userName);
        }

        public async Task<IEnumerable<ChatMessage>> ListMessagesChunksAsync(DateTime date, string userName)
        {
            var result = new List<ChatMessage>();
            var chunk = await _connection.InvokeAsync<ChatMessagesChunk>("ListMessagesChunk", date, userName);
            while (true)
            {
                if (chunk == null || chunk.Messages == null || chunk.Messages.Count() == 0)
                {
                    break;
                }
                result.AddRange(chunk.Messages);
                if (string.IsNullOrEmpty(chunk.Next))
                {
                    break;
                }
                chunk = await _connection.InvokeAsync<ChatMessagesChunk>("ListMessagesNext", chunk.Next, userName);
            }
            return result;
        }

        public async Task<IEnumerable<ChatMessage>> ListMessagesChunksAsync(string last, string userName)
        {
            var result = new List<ChatMessage>();
            var chunk = await _connection.InvokeAsync<ChatMessagesChunk>("ListMessagesNext", last, userName);
            while (true)
            {
                if (chunk == null || chunk.Messages == null || chunk.Messages.Count() == 0)
                {
                    break;
                }
                result.AddRange(chunk.Messages);
                if (string.IsNullOrEmpty(chunk.Next))
                {
                    break;
                }
                chunk = await _connection.InvokeAsync<ChatMessagesChunk>("ListMessagesNext", chunk.Next, userName);
            }
            return result;
        }

        public Task<string> PingAsync()
        {
            return _connection.InvokeAsync<string>("Ping");
        }

        public async Task DisconnectAsync()
        {
            if (_connection is null) return;
            try
            {
                await _connection.StopAsync().ConfigureAwait(false);
                await _connection.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(this, ex);
            }
            finally
            {
                _connection = null;
            }
        }

        public void Dispose()
        {
            _ = DisconnectAsync();
        }
    }
}