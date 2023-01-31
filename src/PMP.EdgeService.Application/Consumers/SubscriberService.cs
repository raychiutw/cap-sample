using DotNetCore.CAP;
using PMP.EdgeService.Common.Constants;
using PMP.EdgeService.Domain.Entities;

namespace PMP.EdgeService.Application.Consumers;

public class SubscriberService : ISubscriberService, ICapSubscribe
{
    [CapSubscribe(AppConstants.GoifNoticeTopic, Group = AppConstants.QueueName)]
    public async Task DeductProductQty(Order order, CancellationToken cancellationToken)
    {
        Console.WriteLine("1:" + order);

        await Task.Yield();
    }
}