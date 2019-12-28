using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using Shared;

namespace ClientBehaviorEmulator
{
    class Host
    {
        static readonly ILog log = LogManager.GetLogger<Host>();

        IEndpointInstance endpoint;

        public string EndpointName => "NServiceBusPlayground.ClientBehaviorEmulator";

        public async Task Start()
        {
            try
            {
                var endpointConfiguration = new EndpointConfiguration(EndpointName);
                var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                transport.ConnectionString("host=rabbitmq");
                transport.UseConventionalRoutingTopology();
                transport.Routing().RouteToEndpoint(
                    messageType: typeof(PlaceOrder), 
                    destination: "NServiceBusPlayground.OrderService"
                );
                
                endpointConfiguration.EnableInstallers();

                endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
                
                var random = new Random();
                
                while (true)
                {
                    var orderId = Guid.NewGuid().ToString();
                    
                    await PlaceOrder(endpoint, orderId);

                    if (random.Next(0,5) == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        await CancelOrder(endpoint, orderId);
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
                
            }
            catch (Exception ex)
            {
                FailFast("Failed to start.", ex);
            }
        }

        public async Task Stop()
        {
            try
            {
                await endpoint?.Stop();
            }
            catch (Exception ex)
            {
                FailFast("Failed to stop correctly.", ex);
            }
        }
        
        private Task PlaceOrder(IMessageSession session, string orderId)
        {
            var command = new PlaceOrder
            {
                OrderId = orderId
            };
            log.Info($"Sending PlaceOrder command, OrderId = {orderId}");
            return session.Send(command);
        }

        private Task CancelOrder(IMessageSession session, string orderId)
        {
            var command = new CancelOrder
            {
                OrderId = orderId
            };
            log.Info($"Sending CancelOrder command,OrderId = {orderId}");
            return session.Send("NServiceBusPlayground.OrderService", command);
        }

        async Task OnCriticalError(ICriticalErrorContext context)
        {
            try
            {
                await context.Stop();
            }
            finally
            {
                FailFast($"Critical error, shutting down: {context.Error}", context.Exception);
            }
        }

        void FailFast(string message, Exception exception)
        {
            try
            {
                log.Fatal(message, exception);
            }
            finally
            {
                Environment.FailFast(message, exception);
            }
        }
    }
}
