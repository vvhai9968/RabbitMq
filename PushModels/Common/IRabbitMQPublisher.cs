namespace Common;

public interface IRabbitMqPublisher<T>
{
    Task PublishMessageAsync(T message);
}