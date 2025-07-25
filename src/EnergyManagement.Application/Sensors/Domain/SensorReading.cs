namespace EnergyManagement.Application.Sensors.Domain;

public class SensorReading
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public double Current { get; set; }
    public double Voltage { get; set; }
    public double Power { get; set; }
    public double Energy { get; set; }
    public int Rssi { get; set; }
    public long FreeHeap { get; set; }
}