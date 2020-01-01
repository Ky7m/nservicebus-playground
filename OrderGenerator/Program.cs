using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderGenerator
{
    internal class Program
    {
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(0);

        private static async Task Main()
        {
            Console.CancelKeyPress += CancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;

            var host = new Host();

            Console.Title = Host.EndpointName;

            await host.Start();

            // wait until notified that the process should exit
            await Semaphore.WaitAsync();

            await host.Stop();
        }

        private static void CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            Semaphore.Release();
        }

        private static void ProcessExit(object sender, EventArgs e)
        {
            Semaphore.Release();
        }
    }
}