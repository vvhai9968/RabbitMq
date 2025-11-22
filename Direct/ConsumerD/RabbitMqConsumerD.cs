using System.Text;
using Common;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ConsumerD;

public class RabbitMqConsumerD(RabbitMqConnectionFactory connectionFactory)
{
    public async Task ConsumeQueueAsync(string queue, string routingKey, Action<string> handleMessage)
    {
        // Tạo một channel mới từ ConnectionFactory (mỗi channel là 1 "đường ống" giao tiếp đến RabbitMQ)
        // Channel là lightweight, nên tạo nhiều cũng không sao.
        // Channel là nơi Producer/Consumer gửi và nhận dữ liệu.
        await using var channel = await connectionFactory.CreateChannelAsync();

        // Khai báo exchange kiểu Direct.
        // Direct exchange gửi message đến queue dựa vào routing-key khớp chính xác.
        // Nếu exchange đã tồn tại thì RabbitMQ sẽ bỏ qua, không tạo lại.
        await channel.ExchangeDeclareAsync("direct_logs", ExchangeType.Direct);

        // Khai báo queue.
        // - queue: tên queue được truyền vào từ hàm
        // - durable = false → queue sẽ không tồn tại sau khi RabbitMQ restart
        // - exclusive = true → chỉ cho phép connection hiện tại sử dụng; khi connection đóng thì queue bị xoá
        // - autoDelete = true → khi consumer cuối cùng cancel/thoát thì queue cũng tự bị xóa
        //
        // => Thường dùng cho worker tạm, log viewer, hoặc consumer chạy thời vụ.
        await channel.QueueDeclareAsync(
            queue: queue,
            durable: false,
            exclusive: true,
            autoDelete: true
        );

        // Bind queue vào exchange theo routingKey.
        // Nghĩa là: chỉ những message có routing-key khớp 100% mới được đưa vào queue này.
        await channel.QueueBindAsync(
            queue: queue,
            exchange: "direct_logs",
            routingKey: routingKey
        );

        Console.WriteLine($"[*] Waiting for logs with routing-key = '{routingKey}'");

        // Tạo consumer dạng async.
        // AsyncEventingBasicConsumer cho phép xử lý message bất đồng bộ.
        var consumer = new AsyncEventingBasicConsumer(channel);

        // Đăng ký event khi nhận được message.
        consumer.ReceivedAsync += async (model, ea) =>
        {
            // Chuyển byte[] message body thành string UTF8
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());

            Console.WriteLine($"[x] Received '{ea.RoutingKey}': {message}");

            // Thực thi callback do caller truyền vào.
            // Nếu không có callback, thì skip.
            handleMessage?.Invoke(message);

            // Vì dùng async delegate nên phải return Task
            await Task.CompletedTask;
        };

        // Bắt đầu consume queue.
        // autoAck = true → RabbitMQ tự coi message đã xử lý xong ngay khi gửi cho consumer.
        // Nếu muốn bảo đảm xử lý thành công mới ack thì để autoAck = false và gọi BasicAck().
        await channel.BasicConsumeAsync(
            queue: queue,
            autoAck: true,
            consumer: consumer
        );

        // Giữ chương trình chạy vô thời hạn để consumer hoạt động liên tục.
        // Nếu không có dòng này, method sẽ kết thúc → channel dispose → consumer bị tắt.
        await Task.Delay(Timeout.Infinite);
    }
}