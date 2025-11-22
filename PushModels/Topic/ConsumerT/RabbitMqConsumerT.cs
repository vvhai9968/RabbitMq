using System.Text;
using Common;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsumerT;

public class RabbitMqConsumerT(RabbitMqConnectionFactory connectionFactory)
{
    public async Task ConsumeQueueAsync(string queue, string bindingPattern, Action<string> handleMessage)
    {
        // 1. Tạo channel để giao tiếp với RabbitMQ
        await using var channel = await connectionFactory.CreateChannelAsync();

        // 2. Khai báo Topic Exchange
        // Topic Exchange cho phép sử dụng wildcard trong routing key:
        //  - *  (match đúng 1 "phần" giữa các dấu . )
        //  - #  (match 0 hoặc nhiều phần)
        //
        // Ví dụ:
        //   "*.info" match: user.info, payment.info
        //   "user.*.deleted" match: user.us.deleted
        //   "error.#" match: error.db, error.db.critical, error.anything.anything
        await channel.ExchangeDeclareAsync("topic_logs", ExchangeType.Topic);

        // 3. Tạo queue tạm (ephemeral queue)
        //    - durable = false → không lưu khi restart
        //    - exclusive = true → chỉ consumer này dùng
        //    - autoDelete = true → consumer tắt → queue bị xóa
        // Đây là kiểu queue đi theo tuổi thọ consumer.
        await channel.QueueDeclareAsync(
            queue: queue,
            durable: false,
            exclusive: true,
            autoDelete: true
        );

        // 4. Bind queue vào Topic Exchange theo pattern
        // bindingPattern có thể chứa wildcard:
        //   vd: "*.error"
        //       "user.#"
        //       "#.warn"
        //
        // Khi Producer publish với routing-key phù hợp pattern này → queue nhận message.
        await channel.QueueBindAsync(
            queue: queue,
            exchange: "topic_logs",
            routingKey: bindingPattern
        );

        Console.WriteLine($"[*] Waiting for logs with topic-pattern = '{bindingPattern}'");

        // 5. Tạo consumer bất đồng bộ
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            // Body message dạng byte[] → decode thành UTF8 string
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());

            // In ra routing-key để thấy message match từ pattern nào
            Console.WriteLine($"[x] Received '{ea.RoutingKey}': {message}");

            // Gọi callback để xử lý message
            handleMessage?.Invoke(message);

            await Task.CompletedTask;
        };

        // 6. Bắt đầu consume từ queue
        // autoAck = true → message coi như xử lý xong ngay
        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: true,
            consumer: consumer
        );

        // 7. Giữ consumer chạy liên tục (không thoát)
        await Task.Delay(Timeout.Infinite);
    }
}