# cap-sample

## 安裝套件

安裝CAP套件

```shell
PM> Install-Package DotNetCore.CAP
```

安裝訊息佇列套件 - 依據實際情況選擇安裝

```shell
PM> Install-Package DotNetCore.CAP.Kafka
PM> Install-Package DotNetCore.CAP.RabbitMQ
PM> Install-Package DotNetCore.CAP.AzureServiceBus
PM> Install-Package DotNetCore.CAP.AmazonSQS
PM> Install-Package DotNetCore.CAP.NATS
PM> Install-Package DotNetCore.CAP.RedisStreams
PM> Install-Package DotNetCore.CAP.Pulsar

```

安裝CAP資料庫套件 - 依據實際情況選擇安裝

```shell
PM> Install-Package DotNetCore.CAP.SqlServer
PM> Install-Package DotNetCore.CAP.MySql
PM> Install-Package DotNetCore.CAP.PostgreSql
PM> Install-Package DotNetCore.CAP.MongoDB
```

使用 SqlServer 可以使用 `UseEntityFramework`, 可使用 transaction 確保商業邏輯與訊息一致

## 範例程式

> Program.cs

```csharp
builder.Services.AddCap(x =>
        {
            // 設定前綴詞
            x.TopicNamePrefix = "cap.pmp.edge";
            x.GroupNamePrefix = "cap.pmp.edge";

            // 使用 EF Core DbContext
            x.UseEntityFramework<CapDbContext>();

            x.UseRabbitMQ(config =>
            {
                var options = builder.Configuration
                    .GetSection("RabbitMqOptions")
                    .Get<RabbitMqOptions>();

                // 帳號, 密碼, 主機, Port, ExchangeName
                config.UserName = options.UserName;
                config.Password = options.Password;
                config.HostName = options.HostName;
                config.Port = options.Port;
                config.ExchangeName = options.ExchangeName;
            });

            x.UseDashboard();

            // retry 次數
            x.FailedRetryCount = 5;

            x.FailedThresholdCallback = failed =>
            {
                var logger = failed.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError($@"A message of type {failed.MessageType} failed after executing {x.FailedRetryCount} several times,
                        requiring manual troubleshooting. Message name: {failed.Message.GetName()}");
            };

            x.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        });
```

> 發送訊息

```csharp
    private readonly ICapPublisher _capBus;

    public PublishController(ICapPublisher capPublisher)
    {
        _capBus = capPublisher;
    }

    [HttpPost("datetime")]
    public IActionResult WithoutTransaction()
    {
        _capBus.Publish("xxx.services.show.time", DateTime.Now);

        return Ok();
    }

    [HttpPost("anonymous")]
    public async Task<IActionResult> AnonymousType()
    {
        await _capBus.PublishAsync(AppConstants.GoifNoticeTopic, new OrderP { OrderId = 1, ProductId = 100, Qty = 20 });

        return Ok();
    }
    
    //EntityFramework 中使用 transaction，自動 commit
    [HttpPost("ef/transaction")]
    public IActionResult EntityFrameworkWithTransaction([FromServices] CapDbContext dbContext)
    {
        using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: true))
        {            
            _capBus.Publish(AppConstants.EdgeDateTimeTopic, DateTime.Now);
        }
        return Ok();
    }
```

> 接收訊息 - 使用 Controller Attribute

```csharp
    [NonAction]
    [CapSubscribe(AppConstants.EdgeDateTimeTopic, Group = AppConstants.QueueName)]
    public void CheckReceivedMessage(DateTime datetime)
    {
        Console.WriteLine(datetime);
    }
```

> 接收訊息 - 一般類別: 繼承 `ICapSubscribe`

>> 定義介面

```csharp
public interface ISubscriberService
{
    Task DeductProductQty(Order order, CancellationToken cancellationToken);
}

public class SubscriberService : ISubscriberService, ICapSubscribe
{
    [CapSubscribe(AppConstants.GoifNoticeTopic, Group = AppConstants.QueueName)]
    public async Task DeductProductQty(Order order, CancellationToken cancellationToken)
    {
        Console.WriteLine("1:" + order);

        await Task.Yield();
    }
}
```

>> 實作

```csharp
public class SubscriberService : ISubscriberService, ICapSubscribe
{
    [CapSubscribe(AppConstants.GoifNoticeTopic, Group = AppConstants.QueueName)]
    public async Task DeductProductQty(Order order, CancellationToken cancellationToken)
    {
        Console.WriteLine("1:" + order);

        await Task.Yield();
    }
}
```

>> 注入設定

```csharp
services.AddScoped<ISubscriberService, SubscriberService>();
```

>> 常數類別

```csharp
public class AppConstants
{
    public const string QueueName = "queue";
    public const string GoifNoticeTopic = "topic.goif.notice";
    public const string EdgeDateTimeTopic = "topic.edge.datetime";
}
```

除了直接強型別轉換, 也可以接收訊息直接處理 JsonElement 物件

```csharp
    [CapSubscribe(AppConstants.GoifNoticeTopic, Group = AppConstants.QueueName)]
    public async Task DeductProductQty(JsonElement param, CancellationToken cancellationToken)
    {
        var order = param.Deserialize<Order>();

        await Task.Yield();
    }
```

## CAP Pubslish To 非 CAP Consumers => 可以

## 非 CAP Publish To CAP Consumer => 需增加 rabbitmq header

```csharp
IModel model = connection.CreateModel();
model.ExchangeDeclare(_headersExchange, ExchangeType.Headers, true);
model.QueueDeclare(_headersQueueOne, true, false, false, null);
Dictionary<string,object> bindingOneHeaders = new Dictionary<string,object>();
bindingOneHeaders.Add("x-match", "all");
bindingOneHeaders.Add("category", "animal");
bindingOneHeaders.Add("type", "mammal");
model.QueueBind(_headersQueueOne, _headersExchange, "", bindingOneHeaders);
```
