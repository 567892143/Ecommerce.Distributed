using MassTransit;
using Shared.Contracts.Commands;
using Shared.Contracts.Events;

namespace OrderService.Api.Sagas;

public class OrderStateMachine : MassTransitStateMachine<OrderSagaState>
{
    public State AwaitingPayment { get; private set; } = default!;
    public State AwaitingInventory { get; private set; } = default!;
    public State Completed { get; private set; } = default!;
    public State Failed { get; private set; } = default!;

    public Event<OrderPlaced> OrderPlacedEvent { get; private set; } = default!;
    public Event<PaymentProcessed> PaymentProcessedEvent { get; private set; } = default!;
    public Event<PaymentFailed> PaymentFailedEvent { get; private set; } = default!;
    public Event<InventoryReserved> InventoryReservedEvent { get; private set; } = default!;
    public Event<InventoryFailed> InventoryFailedEvent { get; private set; } = default!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        // Correlation: every incoming event is matched to a saga instance by its
        // OrderId property. This is what stitches 5 independent messages, from
        // 3 different services, into ONE tracked workflow instance.
        Event(() => OrderPlacedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentProcessedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => PaymentFailedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryReservedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));
        Event(() => InventoryFailedEvent, x => x.CorrelateById(ctx => ctx.Message.OrderId));

        Initially(
            When(OrderPlacedEvent)
                .Then(context =>
                {
                    context.Saga.ProductId = context.Message.ProductId;
                    context.Saga.Quantity = context.Message.Quantity;
                    context.Saga.SubmittedAtUtc = DateTime.UtcNow;
                })
                .TransitionTo(AwaitingPayment));

        During(AwaitingPayment,
            When(PaymentProcessedEvent)
                .Send(new Uri("queue:reserve-inventory"), context => new ReserveInventory
                {
                    OrderId = context.Saga.CorrelationId,
                    ProductId = context.Saga.ProductId,
                    Quantity = context.Saga.Quantity
                })
                .TransitionTo(AwaitingInventory),

            When(PaymentFailedEvent)
                .Then(context => Console.WriteLine($"[Saga] Order {context.Saga.CorrelationId} failed at payment step."))
                .TransitionTo(Failed)
                .Finalize());

        During(AwaitingInventory,
            When(InventoryReservedEvent)
                .Then(context => Console.WriteLine($"[Saga] Order {context.Saga.CorrelationId} completed successfully."))
                .TransitionTo(Completed)
                .Finalize(),

            When(InventoryFailedEvent)
                .Then(context => Console.WriteLine(
                    $"[Saga] Order {context.Saga.CorrelationId} failed at inventory step — " +
                    "PAYMENT WAS ALREADY CHARGED AND IS NOT YET REFUNDED. This is the gap Phase 10 closes."))
                .TransitionTo(Failed)
                .Finalize());

        SetCompletedWhenFinalized();
    }
}