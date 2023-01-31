# cap-sample

## CAP 介紹

CAP 是一個EventBus，同時也是一個在微服務或者SOA系統中解決分佈式事務問題的一個框架。它有助於創建可擴展，可靠並且易於更改的微服務系統。


![架構圖](/imgs/2.png)

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

請在 message header 中加入 `cap-msg-id` `cap-msg-name`

- cap-msg-id: 不重複編號, cap預設使用 Snowflake Id, 也可以用 GUID
- cap-msg-name: 要接收處理的 topic name (在 rabbitmq 稱為 route Key)

![欄位說明](/imgs/1.png)

```csharp
    //Main entry point to the RabbitMQ .NET AMQP client
    var connectionFactory = new ConnectionFactory()
    {
        UserName = "guest",
        Password = "guest",
        HostName = "localhost"
    };

    var connection = connectionFactory.CreateConnection();
    var model = connection.CreateModel();
    var properties = model.CreateBasicProperties();
    properties.Persistent = false;

    // Header 處理
    Dictionary<string, object> dictionary = new Dictionary<string, object>();
    dictionary.Add("cap-msg-id", Guid.NewId().ToString());
    dictionary.Add("cap-msg-name", "topic name(route key)");
    properties.Headers = dictionary;

    byte[] messagebuffer = Encoding.Default.GetBytes("Message to Headers Exchange 'format=pdf' ");
    model.BasicPublish("headers.exchange", "", properties, messagebuffer);
```

## CAP Dashboard

預設路徑 :http://localhost:xxx/cap, 

可以修改預設路徑
```csharp
x.UseDashboard(opt =>{ opt.MatchPath="/mycap"; })
```

可查看訊息發送接收狀態,並手動執行重送

## RabbitMq 後台重送

> header 要加上 `cap-msg-id` `cap-msg-name`

參考附圖可以後台重送

![後台發送](/imgs/3.png)
