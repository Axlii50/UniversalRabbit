using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
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

        public ConsumerManager(IURabbitManager rabbitManager)
        {
            _rabbitManager = rabbitManager;
        }

        public void Subscribe<T>(string queueName, Func<T, Task> onMessageReceived)
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
                        exchange: string.Empty,
                        routingKey: URabbitManager._dlqName,
                        basicProperties: props,
                        body: body,
                        mandatory: true
                    );

                    // Ackujemy oryginalną wiadomość, żeby nie blokowała kolejki
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
            };

            _= channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumerTag: Guid.NewGuid().ToString(),
                noLocal: true,
                exclusive: true,
                arguments: null,
                consumer: consumer).Result;
        }
    }
}
