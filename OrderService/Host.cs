using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace OrderService
{
    internal class Host
    {
        private readonly ILog _log = LogManager.GetLogger<Host>();

        private IEndpointInstance _endpoint;

        public static string EndpointName => "NServiceBusPlayground.OrderService";

        public async Task Start()
        {
            var jittered = Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(500),20, fastFirst: true);
            var jitteredPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(jittered);
            try
            {
                await jitteredPolicy.ExecuteAsync(async () =>
                    {
                        var endpointConfiguration = new EndpointConfiguration(EndpointName);

                        var transport = endpointConfiguration.UseTransport<RabbitMQTransport>();
                        transport.ConnectionString("host=rabbitmq");
                        transport.UseConventionalRoutingTopology();

                        /*
                         * We recommend to only use the .EnableInstallers during a specific Installation/Upgrade stage and not when you would only start the endpoint.
                         * This splits the installation from the actual messages processing which is often required by IT operations.
                         * In general, the installation part uses administrative privileges which are not required when you are processing messages which you only want to run least privilege.
                         */
                        endpointConfiguration.EnableInstallers();
                        _endpoint = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
                    }
                );
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
