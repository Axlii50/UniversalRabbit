using System.Collections.Generic;

namespace URabbit.Config
{
    public class QueueDeclaration
    {
        public string QueueName { get; set; } = default;
        public bool Durable { get; set; }
        public bool Exclusive { get; set; }
        public bool AutoDelete { get; set; }
        public IDictionary<string, object> Arguments { get; set; }
    }
}
