using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

namespace ShippingService
{
    internal class Host
    {
        private readonly ILog _log = LogManager.GetLogger<Host>();

        private IEndpointInstance _endpoint;

        public string EndpointName => "NServiceBusPlayground.ShippingService";

        public async Task Start()
        {
            try
            {
                var endpointConfiguration = new EndpointConfiguration(EndpointName);

                var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                transport.ConnectionString("host=rabbitmq");
                transport.UseConventionalRoutingTopology();
                
                endpointConfiguration.EnableInstallers();

                _endpoint = await Endpoint.Start(endpointConfiguration);
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
