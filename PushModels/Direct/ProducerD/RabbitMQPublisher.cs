using System.Text;
using Common;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace ProducerD;

public class RabbitMqPublisher<T>(RabbitMqConnectionFactory connectionFactory) : IRabbitMqPublisher<T>
{
    public async Task PublishMessageAsync(T message)
    {
        // Tạo channel bất đồng bộ.
        // Channel là cách Producer giao tiếp với RabbitMQ.
        // Channel lightweight → tạo nhiều lần vẫn ok.
        await using var channel = await connectionFactory.CreateChannelAsync();

        // Khai báo exchange kiểu Direct.
        // Direct Exchange gửi message đến đúng queue nào có routing-key khớp EXACT với routing-key Publisher gửi lên.
        // Nếu exchange đã tồn tại thì RabbitMQ sẽ không tạo lại.
        await channel.ExchangeDeclareAsync(
            exchange: "direct_logs",
            type: ExchangeType.Direct
        );

        // =======================
        //      SEND ERROR LOG
        // =======================
        // Routing-key đại diện cho mức độ log: "error"
        string severityError = "error";

        // Ghép message JSON + nội dung thông báo lỗi
        string messageError = $"{JsonConvert.SerializeObject(message)} PANIC: Database is down!";

        // Encode message thành byte[] để gửi qua RabbitMQ
        var bodyError = Encoding.UTF8.GetBytes(messageError);

        // Publish message với routing-key = "error"
        // Queue nào bind routing-key "error" sẽ nhận message này
        await channel.BasicPublishAsync(
            exchange: "direct_logs",
            routingKey: severityError,
            body: bodyError
        );

        Console.WriteLine($" [x] Sent '{severityError}':'{messageError}'");

        // =======================
        //      SEND INFO LOG
        // =======================
        string severityInfo = "info";

        // Thông tin log bình thường
        string messageInfo = $"{JsonConvert.SerializeObject(message)} User logged in.";

        var bodyInfo = Encoding.UTF8.GetBytes(messageInfo);

        // Publish message routing-key = "info"
        await channel.BasicPublishAsync(
            exchange: "direct_logs",
            routingKey: severityInfo,
            body: bodyInfo
        );

        Console.WriteLine($" [x] Sent '{severityInfo}':'{messageInfo}'");
    }
}