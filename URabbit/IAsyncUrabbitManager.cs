using RabbitMQ.Client;
using System.Threading.Tasks;
using System;

namespace URabbit
{
    public interface IAsyncUrabbitManager : IDisposable, IConnection
    {
        Task InitAsync();
        Task<IChannel> CreateAsyncChannel();
    }
}
