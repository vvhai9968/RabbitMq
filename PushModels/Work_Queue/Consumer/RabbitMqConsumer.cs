using System.Text;
using Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer;

public class RabbitMqConsumer(RabbitMqConnectionFactory connectionFactory)
{
    public async Task ConsumeQueueAsync(string queueName, Action<string> handleMessage)
    {
        // 1. Tạo channel để giao tiếp với RabbitMQ
        //    Channel lightweight, mỗi channel có thể lắng nghe nhiều queue
        await using var channel = await connectionFactory.CreateChannelAsync();

        // 2. Khai báo queue y hệt như Producer
        //    - durable = true → queue tồn tại khi RabbitMQ restart
        //    - exclusive = false → nhiều consumer có thể dùng chung queue
        //    - autoDelete = false → queue không tự xóa khi consumer tắt
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        // 3. Tạo consumer bất đồng bộ (Async)
        //    autoAck = false → chúng ta sẽ xác nhận (Ack) thủ công message sau khi xử lý xong
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            // 3a. Lấy message từ body byte[] → string
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [x] Received queue 1 {message}");

            // 3b. Gọi callback xử lý message
            handleMessage(message);

            // 3c. Giả lập công việc nặng (3ms)
            await Task.Delay(3);

            Console.WriteLine(" [x] Done queue 1");

            // 3d. Manual ACK (phần quan trọng!)
            //    - deliveryTag: ID của message trên channel này
            //    - multiple: false → chỉ ACK message này thôi
            // Khi Ack, RabbitMQ biết rằng message đã được xử lý xong, không gửi lại
            await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        // 4. Bắt đầu consume message từ queue
        //    autoAck = false → chúng ta kiểm soát ACK thủ công
        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false, // cực kỳ quan trọng nếu muốn xử lý message an toàn
            consumer: consumer
        );

        // 5. Giữ consumer luôn chạy
        //    Nếu không có dòng này, chương trình kết thúc → consumer bị dispose
        await Task.Delay(Timeout.Infinite);
    }
}
