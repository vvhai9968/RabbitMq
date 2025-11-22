using Common;
using ConsumerF;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddSingleton<RabbitMqConsumerF>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var consumer = app.Services.GetRequiredService<RabbitMqConsumerF>();

_ = consumer.ConsumeQueueAsync("file_log",message => { Console.WriteLine($"[x] Received queue 2: {message}"); });

app.Run();