namespace GlobalSolution.SenseSpot.API.Models;

public class TemperatureSensor : Sensor
{
    public TemperatureSensor()
    {
        SensorType = SensorType.Temperature;
        Unit = "C";
    }
}
