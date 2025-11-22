using System.Text;
using Common;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace ProducerF;

public class RabbitMqPublisher<T>(RabbitMqConnectionFactory connectionFactory) : IRabbitMqPublisher<T>
{
    public async Task PublishMessageAsync(T message)
    {
        // 1. Tạo channel bất đồng bộ
        //    Channel là "đường truyền" để Producer gửi dữ liệu vào RabbitMQ.
        //    Mỗi lần publish có thể tạo channel mới, vì channel rất nhẹ (lightweight).
        await using var channel = await connectionFactory.CreateChannelAsync();

        // 2. Khai báo Fanout Exchange
        //    - Fanout sẽ broadcast message đến TẤT CẢ queue đang bind với exchange.
        //    - Không quan tâm routing-key.
        //    - Nếu exchange tồn tại rồi thì RabbitMQ chỉ bỏ qua, không tạo lại.
        await channel.ExchangeDeclareAsync(
            exchange: "logs_fanout",
            type: ExchangeType.Fanout
        );

        // 3. Convert object message → JSON → byte[] để gửi qua RabbitMQ
        var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

        // 4. Publish message đến exchange fanout
        //    - routingKey = "" (bắt buộc trong fanout, vì fanout ignore routing-key)
        //    - Tất cả queue đã bind vào "logs_fanout" sẽ nhận được message này
        await channel.BasicPublishAsync(
            exchange: "logs_fanout",
            routingKey: "",
            body: body
        );

        // 5. Log ra console để biết message đã được gửi
        Console.WriteLine($" [x] Sent '{message}'");
    }
}