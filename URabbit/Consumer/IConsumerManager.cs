using System;
using System.Threading.Tasks;

namespace URabbit.Consumer
{
    public interface IConsumerManager
    {
        void Subscribe<T>(string queueName, Func<T, Task> onMessageReceived);
    }
}
