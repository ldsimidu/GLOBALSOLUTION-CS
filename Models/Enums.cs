namespace GlobalSolution.SenseSpot.API.Models;

public enum OperationMode
{
    Exploration = 1,
    Economy = 2,
    BlackBox = 3,
    Alert = 4
}

public enum RiskLevel
{
    Safe = 1,
    Attention = 2,
    Critical = 3
}

public enum ConnectionStatus
{
    Online = 1,
    Offline = 2,
    Intermittent = 3
}

public enum SensorType
{
    Temperature = 1,
    Humidity = 2,
    Luminosity = 3,
    AirQuality = 4,
    Vibration = 5
}

public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3
}

public enum RecommendedActionType
{
    Advance = 1,
    Pause = 2,
    Retreat = 3,
    RemoteOnly = 4,
    SendRobot = 5,
    AvoidHumanEntry = 6
}
