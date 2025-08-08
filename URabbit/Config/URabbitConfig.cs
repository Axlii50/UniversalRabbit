using System.Collections.Generic;

namespace URabbit.Config
{
    public class URabbitConfig
    {
        internal List<QueueDeclaration> QueuesToRegister { get; } = new List<QueueDeclaration>();
        public void RegisterQueue<T>(
            string queueName = null,
            bool durable = false,
            bool exclusive = false,
            bool autoDelete = false,
            IDictionary<string, object> arguments = null)
        {
            QueuesToRegister.Add(new QueueDeclaration
            {
                QueueName = queueName ?? typeof(T).Name,
                Durable = durable,
                Exclusive = exclusive,
                AutoDelete = autoDelete,
                Arguments = arguments
            });
        }
    }
}
