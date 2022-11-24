using System.Text;
using Microsoft.Azure.Devices.Client;
using device.Models;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Shared;

DeviceConfigInfo configInfo = new DeviceConfigInfo();
var connectionString = "HostName=tjorvenIOT.azure-devices.net;DeviceId=pctjorven;SharedAccessKey=vhLhIx3dmdN7ptyglg87Qks62ZD6S6m+QYIUQhCZ4wM=";

using var deviceClient = DeviceClient.CreateFromConnectionString(connectionString);


//// open connection explicitly
await deviceClient.OpenAsync();
await deviceClient.SetReceiveMessageHandlerAsync(ReceiveMessage, null);
await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null);
await ForceDeviceTwinRetrieval();

#region Boot
var reportedProperties = new TwinCollection
{
    ["BootTime"] = DateTime.Now
};

await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

#endregion

while (true)
{
    await SendMessage();
    Thread.Sleep(1000);
}

async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object userContext)
{
    Console.WriteLine("One or more device twin desired properties changed:");
    Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));
    configInfo = JsonConvert.DeserializeObject<DeviceConfigInfo>(desiredProperties.ToJson());

}

async Task ForceDeviceTwinRetrieval()
{
    var twin = await deviceClient.GetTwinAsync();
    Console.WriteLine(twin.Properties.Desired.ToJson());
    configInfo = JsonConvert.DeserializeObject<DeviceConfigInfo>(twin.Properties.Desired.ToJson());
    Console.WriteLine("The Devicetwin is forced retrieved");
}
async Task ReceiveMessage(Message message, object userContext)
{
    var messageData = Encoding.ASCII.GetString(message.GetBytes());
    Console.WriteLine("Received message: {0}", messageData);
    await deviceClient.CompleteAsync(message);
}

async Task SendMessage()
{
    TemperatureMessage tpm = new TemperatureMessage();
    Random rnd = new Random();
    tpm.Temperature = rnd.Next(0, 100);
    if (tpm.Temperature > configInfo.threshold)
    {
        var json = JsonConvert.SerializeObject(tpm);
        using var message = new Message(Encoding.UTF8.GetBytes(json));
        Console.WriteLine("Message sent: {0}", message);
        Console.WriteLine("A single message is sent");
        await deviceClient.SendEventAsync(message);
    }

}