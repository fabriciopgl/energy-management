using System.Text.Json.Serialization;

namespace EnergyManagement.Application.Sensors.Models.Dtos;

public class SensorReadingDto
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("current")]
    public double Current { get; set; }

    [JsonPropertyName("voltage")]
    public double Voltage { get; set; }

    [JsonPropertyName("power")]
    public double Power { get; set; }

    [JsonPropertyName("energy")]
    public double Energy { get; set; }

    [JsonPropertyName("rssi")]
    public int Rssi { get; set; }

    [JsonPropertyName("freeHeap")]
    public long FreeHeap { get; set; }
}