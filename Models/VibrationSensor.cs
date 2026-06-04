namespace GlobalSolution.SenseSpot.API.Models;

public class VibrationSensor : Sensor
{
    public VibrationSensor()
    {
        SensorType = SensorType.Vibration;
        Unit = "mm/s";
    }
}
