namespace GlobalSolution.SenseSpot.API.Models;

public class AirQualitySensor : Sensor
{
    public AirQualitySensor()
    {
        SensorType = SensorType.AirQuality;
        Unit = "aqi";
    }
}
