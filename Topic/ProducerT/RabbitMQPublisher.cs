using System.Text;
using Common;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace ProducerT;

public class RabbitMqPublisher<T>(RabbitMqConnectionFactory connectionFactory) : IRabbitMqPublisher<T>
{
    public async Task PublishMessageAsync(T message)
    {
        // 1. Tạo channel bất đồng bộ để giao tiếp với RabbitMQ
        //    Channel lightweight → có thể tạo nhiều lần.
        await using var channel = await connectionFactory.CreateChannelAsync();

        // 2. Khai báo Topic Exchange
        //    - Topic Exchange cho phép dùng wildcard trong routing-key:
        //      *  → match đúng 1 phần giữa các dấu .
        //      #  → match 0 hoặc nhiều phần
        await channel.ExchangeDeclareAsync("topic_logs", ExchangeType.Topic);

        // -----------------------
        // 3. Gửi các message với routing-key khác nhau
        // -----------------------

        // --- Message 1 ---
        var routingKey1 = "user.created.usa"; // 3 phần: user.created.usa
        var message1 = $"{message} New user from USA";
        var body1 = Encoding.UTF8.GetBytes(message1);

        // Publish message đến Topic Exchange
        // Queue nào bind với pattern phù hợp routingKey này sẽ nhận được message
        await channel.BasicPublishAsync(
            exchange: "topic_logs",
            routingKey: routingKey1,
            body: body1
        );
        Console.WriteLine($"[x] Sent '{routingKey1}' : {message1}");

        // --- Message 2 ---
        var routingKey2 = "user.deleted.eur";
        var message2 = $"{message} User from EUR deleted";
        var body2 = Encoding.UTF8.GetBytes(message2);

        await channel.BasicPublishAsync(
            exchange: "topic_logs",
            routingKey: routingKey2,
            body: body2
        );
        Console.WriteLine($"[x] Sent '{routingKey2}' : {message2}");

        // --- Message 3 ---
        var routingKey3 = "payment.failed.usa";
        var message3 = $"{message} USA payment failed";
        var body3 = Encoding.UTF8.GetBytes(message3);

        await channel.BasicPublishAsync(
            exchange: "topic_logs",
            routingKey: routingKey3,
            body: body3
        );
        Console.WriteLine($"[x] Sent '{routingKey3}' : {message3}");
        
        // --- Message 4 ---
        var routingKey4 = "failed.usa"; // 2 phần: failed.usa
        var message4 = $"{message} USA payment failed";
        var body4 = Encoding.UTF8.GetBytes(message4);

        await channel.BasicPublishAsync(
            exchange: "topic_logs",
            routingKey: routingKey4,
            body: body4
        );
        Console.WriteLine($"[x] Sent '{routingKey4}' : {message4}");

        // Lưu ý:
        // - Queue bind pattern như "*.created.*" sẽ chỉ nhận message1
        // - Queue bind pattern như "user.#" sẽ nhận message1 và message2
        // - Queue bind pattern như "#.usa" sẽ nhận message1, message3, message4
        // - Queue bind pattern như "payment.*.*" sẽ chỉ nhận message3
    }
}
