using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using Shared;

namespace ClientBehaviorEmulator
{
    internal class Host
    {
        private readonly ILog _log = LogManager.GetLogger<Host>();

        private IEndpointInstance _endpoint;

        public static string EndpointName => "NServiceBusPlayground.ClientBehaviorEmulator";

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

                _endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
                
                var random = new Random();
                
                while (true)
                {
                    var orderId = Guid.NewGuid().ToString();
                    
                    await PlaceOrder(_endpoint, orderId);

                    if (random.Next(0,5) == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        await CancelOrder(_endpoint, orderId);
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
                await _endpoint?.Stop();
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
            _log.Info($"Sending PlaceOrder command, OrderId = {orderId}");
            return session.Send(command);
        }

        private Task CancelOrder(IMessageSession session, string orderId)
        {
            var command = new CancelOrder
            {
                OrderId = orderId
            };
            _log.Info($"Sending CancelOrder command,OrderId = {orderId}");
            return session.Send("NServiceBusPlayground.OrderService", command);
        }

        private void FailFast(string message, Exception exception)
        {
            try
            {
                _log.Fatal(message, exception);
            }
            finally
            {
                Environment.FailFast(message, exception);
            }
        }
    }
}
