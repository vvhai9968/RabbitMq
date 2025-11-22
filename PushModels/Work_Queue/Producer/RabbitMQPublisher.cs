using System.Text;
using Common;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Producer;

public class RabbitMqPublisher<T>(RabbitMqConnectionFactory connectionFactory) : IRabbitMqPublisher<T>
{
    public async Task PublishMessageAsync(T message)
    {
        // 1. Tạo channel bất đồng bộ
        //    Channel là "đường ống" để gửi message đến RabbitMQ
        await using var channel = await connectionFactory.CreateChannelAsync();

        const string queueName = "Work_Queue";

        // 2. Khai báo Queue (Durable Queue)
        //    - durable = true → queue tồn tại sau khi RabbitMQ restart
        //    - exclusive = false → nhiều consumer có thể dùng chung queue
        //    - autoDelete = false → queue không tự xóa khi consumer tắt
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,  
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        // 3. Chuyển object message sang JSON → byte[]
        var data = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(data);

        // 4. Đánh dấu tin nhắn là 'persistent' (bền vững)
        //    - DeliveryMode = 2 → tin nhắn không bị mất khi RabbitMQ restart
        //    - Nếu không set → mặc định transient → dễ mất khi RabbitMQ restart
        var properties = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent
        };

        // 5. Publish message đến Default Exchange
        //    - Default Exchange là exchange rỗng ("")
        //    - RoutingKey = tên queue → message sẽ trực tiếp vào queue tương ứng
        //    - mandatory = false → nếu queue không tồn tại thì bỏ message
        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body
        );

        Console.WriteLine($" [x] Sent {data}");
    }
}