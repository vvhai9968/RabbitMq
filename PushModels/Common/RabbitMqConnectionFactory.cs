using RabbitMQ.Client;

namespace Common;

public class RabbitMqConnectionFactory
{
    public async Task<IConnection> CreateConnectionAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "admin",
            Password = "admin"
        };

        return await factory.CreateConnectionAsync();
    }

    public async Task<IChannel> CreateChannelAsync()
    {
        var connection = await CreateConnectionAsync();
        return await connection.CreateChannelAsync();
    }
}