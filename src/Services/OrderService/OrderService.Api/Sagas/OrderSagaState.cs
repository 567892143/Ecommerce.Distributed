using MassTransit;

namespace OrderService.Api.Sagas;

public class OrderSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }   // == OrderId, always
    public string CurrentState { get; set; } = default!;
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
}