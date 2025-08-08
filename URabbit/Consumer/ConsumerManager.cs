using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using URabbit.ErrorHandling;

namespace URabbit.Consumer
{
    public class ConsumerManager : IConsumerManager
    {
        private readonly IURabbitManager _rabbitManager;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly IMessageErrorHandler _errorHandler;

        public ConsumerManager(IURabbitManager rabbitManager)
        {
            _rabbitManager = rabbitManager;

            // Retry tylko dla wybranych wyjątków
            _retryPolicy = Policy
                .Handle<TimeoutException>()
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(
                    retryCount: 10,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)),
                    onRetry: (exception, timespan, attempt, context) =>
                    {
                        Console.WriteLine($"[Rabbit] Próba {attempt} po błędzie {exception.GetType().Name}: {exception.Message}");
                    });

            _errorHandler = new RetryOnTimeoutHandler();
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

                    if (message == null)
                        throw new JsonException("Nie można zdeserializować wiadomości.");

                    try
                    {
                        // Retry policy tylko na TimeoutException i HttpRequestException
                        await _retryPolicy.ExecuteAsync(async () =>
                        {
                            await onMessageReceived(message);
                        });

                        await channel.BasicAckAsync(ea.DeliveryTag, false);
                    }
                    catch (Exception ex) when (ex is TimeoutException || ex is HttpRequestException)
                    {
                        // Po 10 próbach Polly rzuci ten wyjątek dalej
                        Console.WriteLine($"[Rabbit] Po 10 próbach nadal błąd: {ex.Message}");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false); // wrzucamy do DLQ
                    }
                }
                catch (Exception ex)
                {
                    // Inne błędy od razu idą do DLQ
                    Console.WriteLine($"[Rabbit] Błąd krytyczny, wysyłam od razu do DLQ: {ex.Message}");
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            _ = channel.BasicConsumeAsync(
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
