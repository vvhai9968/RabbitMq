using Common;
using ConsumerD;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddSingleton<RabbitMqConsumerD>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var consumer = app.Services.GetRequiredService<RabbitMqConsumerD>();

_ = consumer.ConsumeQueueAsync("error","error", message => { Console.WriteLine($"[x] Received queue 1: {message}"); });

app.Run();