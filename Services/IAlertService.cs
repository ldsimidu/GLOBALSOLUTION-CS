using GlobalSolution.SenseSpot.API.Models;

namespace GlobalSolution.SenseSpot.API.Services;

public interface IAlertService
{
    Alert? BuildAutomaticAlert(Device device, Sensor sensor, SensorReading reading);
}
