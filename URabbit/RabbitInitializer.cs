using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
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

            foreach (var queue in config.QueuesToRegister)
            {
                //TODO Przetestować czy channel nie moze byc robiony dla całego foreach zamiast za kazdym razem nowy
                using (var channel = await rabbitManager.CreateAsyncChannel())
                    await channel.QueueDeclareAsync(
                        queue: queue.QueueName,
                        durable: queue.Durable,
                        exclusive: queue.Exclusive,
                        autoDelete: queue.AutoDelete,
                        arguments: queue.Arguments
                    );
            }
        }

        // --- Bez DI: Synchronicznie ---
        public static void InitRabbit(IURabbitManager rabbitManager, URabbitConfig config)
        {
            if (rabbitManager == null || config == null)
                throw new ArgumentNullException("Rabbit manager or config cannot be null.");

            rabbitManager.Init();

            foreach (var queue in config.QueuesToRegister)
            {
                using (var channel = rabbitManager.CreateChannel())
                    _ = channel.QueueDeclareAsync(
                        queue: queue.QueueName,
                        durable: queue.Durable,
                        exclusive: queue.Exclusive,
                        autoDelete: queue.AutoDelete,
                        arguments: queue.Arguments
                    ).Result;
            }
        }
    }

}

