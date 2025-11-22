using System.Text;
using Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsumerF;

public class RabbitMqConsumerF(RabbitMqConnectionFactory connectionFactory)
{
    public async Task ConsumeQueueAsync(string queueName, Action<string> handleMessage)
    {
        // 1. Tạo channel để giao tiếp với RabbitMQ
        // Channel là nơi Producer/Consumer gửi và nhận dữ liệu.
        await using var channel = await connectionFactory.CreateChannelAsync();

        // 2. Khai báo exchange kiểu Fanout
        // Fanout exchange sẽ gửi (broadcast) message tới TẤT CẢ các queue được bind vào nó.
        // Không quan tâm routing-key (routing-key luôn bị bỏ qua trong Fanout).
        const string exchangeName = "logs_fanout";

        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Fanout,
            durable: false // Exchange không tồn tại sau khi RabbitMQ restart
        );

        // 3. Khai báo queue cho consumer
        // Nếu nhiều consumer dùng chung queueName → tất cả cùng đọc chung queue (chia lượt)
        // Nếu mỗi consumer cần nhận TẤT CẢ message → phải dùng queueName khác nhau
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,   // Queue không được lưu khi RabbitMQ restart
            exclusive: false, // false → queue có thể bị nhiều consumer sử dụng
            autoDelete: false // false → queue không tự xóa khi consumer dừng
        );

        // 4. Bind queue vào exchange fanout
        // routingKey không có tác dụng trong fanout, nên để chuỗi rỗng
        await channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: ""
        );

        // 5. Tạo consumer bất đồng bộ để lắng nghe message
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            // Giải mã message từ byte[] sang UTF8 string
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"[x] Received: {message}");

            // Gọi callback mà người dùng truyền vào để xử lý message
            handleMessage?.Invoke(message);

            await Task.CompletedTask;
        };

        // 6. Bắt đầu consume message từ queue
        // autoAck = true → RabbitMQ tự đánh dấu message đã xử lý ngay khi gửi xuống consumer
        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: true,
            consumer: consumer
        );

        // 7. Giữ cho consumer luôn chạy
        await Task.Delay(Timeout.Infinite);
    }
}
