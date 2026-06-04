namespace GlobalSolution.SenseSpot.API.Models;

public class LuminositySensor : Sensor
{
    public LuminositySensor()
    {
        SensorType = SensorType.Luminosity;
        Unit = "lux";
    }
}
