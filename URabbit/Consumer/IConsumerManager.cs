using System;
using System.Threading.Tasks;

namespace URabbit.Consumer
{
    public interface IConsumerManager
    {
        string Subscribe<T>(Func<T, Task> onMessageReceived);
        Task Unsubscribe(string consumerTag);
    }
}
