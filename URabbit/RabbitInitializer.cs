using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
using URabbit.Config;

namespace URabbit
{
    public static class RabbitInitializer
    {

        // --- DI: Asynchronicznie ---
        public static async Task InitRabbitAsync(IServiceProvider provider)
        {
            var rabbitManager = provider.GetService<IAsyncUrabbitManager>();
            var config = provider.GetService<URabbitConfig>();

            if (rabbitManager == null || config == null)
                throw new InvalidOperationException("Rabbit services not registered.");

            await InitRabbitAsync(rabbitManager, config);
        }

        // --- DI: Synchronicznie ---
        public static void InitRabbit(IServiceProvider provider)
        {
            var rabbitManager = provider.GetService<IURabbitManager>();
            var config = provider.GetService<URabbitConfig>();

            if (rabbitManager == null || config == null)
                throw new InvalidOperationException("Rabbit services not registered.");

            InitRabbit(rabbitManager, config);
        }

        // --- Bez DI: Asynchronicznie ---
        public static async Task InitRabbitAsync(IAsyncUrabbitManager rabbitManager, URabbitConfig config)
        {
            if (rabbitManager == null || config == null)
                throw new ArgumentNullException("Rabbit manager or config cannot be null.");

            await rabbitManager.InitAsync();

            using (var channel = await rabbitManager.CreateAsyncChannel())
            {
                foreach (var queue in config.QueuesToRegister)
                {
                    var mainQueueName = queue.QueueName;
                    var dlqName = $"{mainQueueName}.DLQ";

                    // Tworzymy DLQ (jeśli nie istnieje)
                    await channel.QueueDeclareAsync(
                        queue: dlqName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    );

                    // Kopiujemy argumenty z konfiguracji, jeśli są
                    var mainQueueArgs = queue.Arguments != null
                        ? new Dictionary<string, object>(queue.Arguments)
                        : new Dictionary<string, object>();

                    // Dodajemy konfigurację DLQ
                    mainQueueArgs["x-dead-letter-exchange"] = ""; // domyślny exchange
                    mainQueueArgs["x-dead-letter-routing-key"] = dlqName;

                    // Tworzymy kolejkę główną
                    await channel.QueueDeclareAsync(
                        queue: mainQueueName,
                        durable: queue.Durable,
                        exclusive: queue.Exclusive,
                        autoDelete: queue.AutoDelete,
                        arguments: mainQueueArgs
                    );

                    Console.WriteLine($"[Rabbit] Kolejka '{mainQueueName}' została zarejestrowana z DLQ '{dlqName}'.");
                }
            }
        }

        // --- Bez DI: Synchronicznie ---
        public static void InitRabbit(IURabbitManager rabbitManager, URabbitConfig config)
        {
            if (rabbitManager == null || config == null)
                throw new ArgumentNullException("Rabbit manager or config cannot be null.");

            rabbitManager.Init();

            using (var channel = rabbitManager.CreateChannel())
            {
                foreach (var queue in config.QueuesToRegister)
                {
                    // Nazwa głównej kolejki
                    var mainQueueName = queue.QueueName;

                    // Nazwa DLQ
                    var dlqName = $"{mainQueueName}.DLQ";

                    // Tworzymy DLQ
                    _ = channel.QueueDeclareAsync(
                        queue: dlqName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    ).Result;

                    // Argumenty z konfiguracji + DLQ binding
                    var mainQueueArgs = queue.Arguments != null
                        ? new Dictionary<string, object>(queue.Arguments)
                        : new Dictionary<string, object>();

                    mainQueueArgs["x-dead-letter-exchange"] = ""; // default direct exchange
                    mainQueueArgs["x-dead-letter-routing-key"] = dlqName;

                    // Tworzymy kolejkę główną z podpiętym DLQ
                    _ = channel.QueueDeclareAsync(
                        queue: mainQueueName,
                        durable: queue.Durable,
                        exclusive: queue.Exclusive,
                        autoDelete: queue.AutoDelete,
                        arguments: mainQueueArgs
                    ).Result;

                    Console.WriteLine($"[Rabbit] Kolejka '{mainQueueName}' z DLQ '{dlqName}' została zarejestrowana.");
                }
            }
        }
    }
}

