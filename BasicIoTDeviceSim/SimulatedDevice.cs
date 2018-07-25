using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace BasicIoTDeviceSim
{
	public class SimulatedDevice : IDisposable
	{
		private const string LoopDelayPropertyName = "delay";
		private const int DefaultLoopDelay = 10000;

		private readonly DeviceClient _client;
		private readonly Random _random;

		private int _loopDelay = 10000;
		private int _lastValue = 2000;

		private SimulatedDevice(string connectionString)
		{
			_random = new Random();
			_client = DeviceClient.CreateFromConnectionString(connectionString);
		}

		private Task<MethodResponse> HandleMessage(MethodRequest methodRequest, object userContext)
		{
			string json = methodRequest.DataAsJson;

			try
			{
				// Messages sent from the portal may be double encoded
				if (json.StartsWith('"') && json.EndsWith('"'))
				{
					json = JsonConvert.DeserializeObject<string>(json);
				}

				LogMessage message = JsonConvert.DeserializeObject<LogMessage>(json);

				Console.WriteLine(Environment.NewLine + message.Text);
			}
			catch (JsonReaderException)
			{
				Console.WriteLine(Environment.NewLine + "Unable to deserialize log direct message");

				return Task.FromResult(new MethodResponse(new byte[0], 400));
			}

			return Task.FromResult(new MethodResponse(new byte[0], 200));
		}

		private async Task DesiredPropertyUpdate(TwinCollection desiredProperties, object userContext)
		{
			Console.WriteLine(Environment.NewLine + "Updating device properties");

			int desiredLoopDelay = _loopDelay;

			if (desiredProperties != null &&
				desiredProperties.Contains(LoopDelayPropertyName) &&
				int.TryParse(desiredProperties[LoopDelayPropertyName].ToString(), out desiredLoopDelay))
			{
				_loopDelay = desiredLoopDelay;

				Console.WriteLine($"Set delay to {_loopDelay}ms");
			}
			else
			{
				_loopDelay = DefaultLoopDelay;

				Console.WriteLine($"Couldn't find a valid loop delay in desired properties, set delay to  {_loopDelay}ms");
			}

			TwinCollection reportedProperties = new TwinCollection
			{
				[LoopDelayPropertyName] = _loopDelay
			};

			await _client.UpdateReportedPropertiesAsync(reportedProperties);

			Console.WriteLine("Updated reported properties");
		}

		public async Task Connect()
		{
			await _client.SetMethodHandlerAsync("log", HandleMessage, null);
			await _client.SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdate, null);
			await _client.OpenAsync();

			Twin twin = await _client.GetTwinAsync();

			await DesiredPropertyUpdate(twin.Properties.Desired, null);
		}

		public static async Task<SimulatedDevice> ConnectUsingConnectionStringAsync(string connectionString)
		{
			SimulatedDevice device = new SimulatedDevice(connectionString);

			await device.Connect();

			return device;
		}

		public async Task SimulateLoopAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await Process();

				Console.Write('.');

				await Task.Delay(_loopDelay, cancellationToken);
			}
		}

		private async Task Process()
		{
			int value = _lastValue + _random.Next(-200, 200);

			if (value < 800)
			{
				value = 800;
			}
			else if (value > 4000)
			{
				value = 4000;
			}

			_lastValue = value;

			Message message = MessageHelpers.ToJsonMessage(new
			{
				temperature = value
			});

			await _client.SendEventAsync(message);
		}

		public void Dispose()
		{
			_client.Dispose();
		}
	}
}