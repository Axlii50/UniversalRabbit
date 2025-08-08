namespace URabbit
{
    public interface IConnection
    {
        RabbitMQ.Client.IConnection GetConnection();
    }
}
