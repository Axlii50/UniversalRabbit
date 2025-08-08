using RabbitMQ.Client;
using System;

namespace URabbit
{
    public interface IURabbitManager : IDisposable, IConnection
    {
        void Init();      // dla CreateConnection
        IChannel CreateChannel();
    }
}
