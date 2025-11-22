using Common;
using Consumer;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddSingleton<RabbitMqConsumer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var consumer = app.Services.GetRequiredService<RabbitMqConsumer>();

_ = consumer.ConsumeQueueAsync("task_queue", message =>
{
    Console.WriteLine($"[x] Received queue 2: {message}");
});

app.Run();