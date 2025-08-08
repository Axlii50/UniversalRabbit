using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace URabbit
{
    /// <summary>
    /// "RabbitMQ": {
    ///  "HostName": "localhost",
    ///  "UserName": "guest",
    ///  "Password": "guest",
    ///  "Port": "5672"
    ///}
    /// </summary>

    public class URabbitManager : IURabbitManager, IAsyncUrabbitManager
    {

        public static readonly string _dlqName = "DLQ";
        private readonly ConnectionFactory _factory;
        private RabbitMQ.Client.IConnection _connection;

        public URabbitManager(IConfiguration configuration)
        {
            _factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:HostName"] ?? "localhost",
                UserName = configuration["RabbitMQ:UserName"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest",
                Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672"),
            };
        }
        public URabbitManager(string HostName = null, string UserName = null, string Password = null, int? Port = null)
        {
            _factory = new ConnectionFactory
            {
                HostName = HostName ?? "localhost",
                UserName = UserName ?? "guest",
                Password = Password ?? "guest",
                Port = Port ?? 5672,
            };
        }

        public async Task InitAsync()
        {
            _factory.ConsumerDispatchConcurrency = 1;

            if (_connection == null || !_connection.IsOpen)
                _connection = await _factory.CreateConnectionAsync();
        }

        public void Init()
        {
            _factory.ConsumerDispatchConcurrency = 0;

            if (_connection == null || !_connection.IsOpen)
                _connection = _factory.CreateConnectionAsync().Result;
        }

        public RabbitMQ.Client.IConnection GetConnection()
        {
            if (_connection == null)
                throw new InvalidOperationException("Connection not initialized. Call Init() or InitAsync() first.");

            return _connection;
        }

        public IChannel CreateChannel()
        {
            return GetConnection().CreateChannelAsync().Result;
        }

        public async Task<IChannel> CreateAsyncChannel()
        {
            return await GetConnection().CreateChannelAsync();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.IsOpen)
                    _connection.CloseAsync().Wait();

                _connection.Dispose();
            }
        }
    }
}
