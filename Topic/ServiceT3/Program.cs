using Common;
using ConsumerT;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddSingleton<RabbitMqConsumerT>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var consumer = app.Services.GetRequiredService<RabbitMqConsumerT>();

_ = consumer.ConsumeQueueAsync("payment","payment.*.*", message => { Console.WriteLine($"[x] Received queue 1: {message}"); });

app.Run();