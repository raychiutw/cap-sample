using PMP.EdgeService.Domain.Entities;

namespace PMP.EdgeService.Application.Consumers;

public interface ISubscriberService
{
    Task DeductProductQty(Order order, CancellationToken cancellationToken);
}