using Newtonsoft.Json;

namespace BasicIoTDeviceSim
{
	public class LogMessage
	{
		[JsonProperty("text")]
		public string Text { get; set; }
	}
}
