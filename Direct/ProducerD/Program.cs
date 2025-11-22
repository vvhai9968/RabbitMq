using Common;
using ProducerD;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddSingleton(typeof(IRabbitMqPublisher<>), typeof(RabbitMqPublisher<>));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/task_queue", async (IRabbitMqPublisher<string> publisher) =>
{

    for (int i = 0; i < 600; i++)
    {
        await Task.Delay(2);
        await publisher.PublishMessageAsync($"Haivv {i}");
    }

    return "Done";
}).DisableAntiforgery();

app.Run();