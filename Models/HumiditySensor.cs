namespace GlobalSolution.SenseSpot.API.Models;

public class HumiditySensor : Sensor
{
    public HumiditySensor()
    {
        SensorType = SensorType.Humidity;
        Unit = "%";
    }
}
