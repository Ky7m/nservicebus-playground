using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

namespace ShippingService
{
    class Host
    {
        static readonly ILog log = LogManager.GetLogger<Host>();

        IEndpointInstance endpoint;

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

                endpoint = await Endpoint.Start(endpointConfiguration);
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
