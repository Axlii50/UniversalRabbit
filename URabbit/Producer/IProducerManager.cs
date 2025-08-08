using System.Threading.Tasks;

namespace URabbit.Producer
{
    public interface IProducerManager
    {
        void Publish<T>(T message);
    }
}
