using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using Shared;

namespace OrderService
{
    public class OrderHandler : IHandleMessages<PlaceOrder>, IHandleMessages<CancelOrder>
    {
        static ILog log = LogManager.GetLogger<OrderHandler>();
        static Random _random = new Random();
        public async Task Handle(PlaceOrder message, IMessageHandlerContext context)
        {
            log.Info($"Received PlaceOrder, OrderId = {message.OrderId}");

            // This is normally where some business logic would occur
            await Task.Delay(TimeSpan.FromSeconds(5));

            // test throwing transient exceptions
            if (_random.Next(0, 5) == 0)
            {
                //throw new Exception("Oops:" + message.OrderId);
            }

            var orderPlaced = new OrderPlaced
            {
                OrderId = message.OrderId
            };

            log.Info($"Publishing OrderPlaced, OrderId = {message.OrderId}");

            await context.Publish(orderPlaced);
        }
        
        public Task Handle(CancelOrder message, IMessageHandlerContext context)
        {
            log.Info($"Received CancelOrder, OrderId = {message.OrderId}");
            return Task.CompletedTask;
        }
    }
}