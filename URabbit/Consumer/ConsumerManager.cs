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
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                        await onMessageReceived(message);

                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Rabbit] Error processing message: {ex.Message}");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
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
