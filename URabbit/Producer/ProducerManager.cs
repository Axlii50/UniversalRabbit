using RabbitMQ.Client;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace URabbit.Producer
{
    public class ProducerManager : IProducerManager, IAsyncProducerManager
    {
        private readonly IURabbitManager _rabbitManager;
        private readonly IAsyncUrabbitManager _asyncRabbitManager;

        public ProducerManager(IURabbitManager rabbitManager)
        {
            _rabbitManager = rabbitManager;
        }

        public ProducerManager(IAsyncUrabbitManager rabbitManager)
        {
            _asyncRabbitManager = rabbitManager;
        }

        public void Publish<T>(T message)
        {
            using (var channel = _rabbitManager.CreateChannel())
            {
                channel.ExchangeDeclareAsync(exchange: typeof(T).Name, type: ExchangeType.Direct, durable: true).Wait();

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: typeof(T).Name,
                    body: body
                );
            }
        }

        public async Task PublishAsync<T>(T message)
        {
            using (var channel = await _asyncRabbitManager.CreateAsyncChannel())
            {
                channel.ExchangeDeclareAsync(exchange: typeof(T).Name, type: ExchangeType.Direct, durable: true).Wait();

                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: typeof(T).Name,
                    body: body
                );
            }
        }
    }
}
