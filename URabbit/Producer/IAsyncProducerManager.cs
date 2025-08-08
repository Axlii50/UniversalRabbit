using System.Threading.Tasks;

namespace URabbit.Producer
{
    public interface IAsyncProducerManager
    {
        Task PublishAsync<T>(T message);
    }
}
