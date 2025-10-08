using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace URabbit.Consumer
{
    public class ConsumerManager : IConsumerManager
    {
        private readonly IURabbitManager _rabbitManager;
        private readonly ConcurrentDictionary<string, IChannel> _channels = new ConcurrentDictionary<string, IChannel>();

        public ConsumerManager(IURabbitManager rabbitManager)
        {
            _rabbitManager = rabbitManager;
        }

        public string Subscribe<T>(Func<T, Task> onMessageReceived)
        {
            var channel = _rabbitManager.CreateChannel();

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                try
                {
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                        await onMessageReceived(message);

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Rabbit] Nie udało się przetworzyć wiadomości po wszystkich próbach: {ex.Message}");

                    var props = new BasicProperties();
                    props.Headers = new Dictionary<string, object>
                    {
                        { "ErrorType", ex.GetType().Name },
                        { "ErrorMessage", ex.Message },
                        { "StackTrace", ex.StackTrace }
                    };

                    // Wysyłka do DLQ
                    await channel.BasicPublishAsync(
                        exchange: typeof(T).Name,
                        routingKey: URabbitManager._dlqName,
                        basicProperties: props,
                        body: body,
                        mandatory: true
                    );

                    // Ackujemy oryginalną wiadomość, żeby nie blokowała kolejki
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };

            var consumerTag = Guid.NewGuid().ToString();
            _channels[consumerTag] = channel;

            _ = channel.BasicConsumeAsync(
                queue: typeof(T).Name,
                autoAck: false,
                consumerTag: consumerTag,
                noLocal: true,
                exclusive: true,
                arguments: null,
                consumer: consumer).Result;

            return consumerTag;
        }

        public async Task Unsubscribe(string consumerTag)
        {
            if (_channels.TryRemove(consumerTag, out var channel))
            {
                await channel.BasicCancelAsync(consumerTag);
                await channel.CloseAsync();
                channel.Dispose();
            }
        }
    }
}
