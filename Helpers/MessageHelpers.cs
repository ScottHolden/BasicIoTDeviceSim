using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;

namespace BasicIoTDeviceSim
{
    public static class MessageHelpers
    {
		public static Message ToJsonMessage(object o)
		{
			string json = JsonConvert.SerializeObject(o);

			return new Message(Encoding.UTF8.GetBytes(json));
		}
    }
}
