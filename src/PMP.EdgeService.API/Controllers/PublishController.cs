using DotNetCore.CAP;
using Microsoft.AspNetCore.Mvc;
using PMP.EdgeService.Common.Constants;
using PMP.EdgeService.Persistence;

namespace PMP.EdgeService.API.Controllers;

[ApiController]
[Route("[controller]")]
public class PublishController : ControllerBase
{
    private readonly ICapPublisher _capBus;

    public PublishController(ICapPublisher capPublisher)
    {
        _capBus = capPublisher;
    }

    //不使用事务
    [HttpPost("without/transaction")]
    public IActionResult WithoutTransaction()
    {
        _capBus.Publish(AppConstants.EdgeDateTimeTopic, DateTime.Now);

        return Ok();
    }

    [HttpPost("anonymous")]
    public async Task<IActionResult> AnonymousType()
    {
        await _capBus.PublishAsync(AppConstants.GoifNoticeTopic, new OrderP { OrderId = 1, ProductId = 100, Qty = 20 });

        return Ok();
    }

    //EntityFramework 中使用事务，自动提交
    [HttpPost("ef/transaction")]
    public IActionResult EntityFrameworkWithTransaction([FromServices] CapDbContext dbContext)
    {
        using (var trans = dbContext.Database.BeginTransaction(_capBus, autoCommit: true))
        {
            //业务代码
            _capBus.Publish(AppConstants.EdgeDateTimeTopic, DateTime.Now);
        }
        return Ok();
    }

    [NonAction]
    [CapSubscribe(AppConstants.EdgeDateTimeTopic, Group = AppConstants.QueueName)]
    public void CheckReceivedMessage(DateTime datetime)
    {
        Console.WriteLine(datetime);
    }
}

public class OrderP
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Qty { get; set; }
}