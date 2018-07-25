using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace BasicIoTDeviceSim
{
	public class Program
    {
		static void Main(string[] args)
		{
			using (CancellationTokenSource cts = new CancellationTokenSource())
			{

				Console.CancelKeyPress += (s, e) =>
				{
					e.Cancel = true;
					cts.Cancel();
				};

				string connectionString = ConfigurationManager.AppSettings["deviceconnectionstring"];

				SimulateDeviceAsync(connectionString, cts.Token).Wait();
			}
		}

		static async Task SimulateDeviceAsync(string connectionString, CancellationToken cancellationToken)
		{
			using (SimulatedDevice device = await SimulatedDevice.ConnectUsingConnectionStringAsync(connectionString))
			{
				await device.SimulateLoopAsync(cancellationToken);
			}
		}
	}
}
