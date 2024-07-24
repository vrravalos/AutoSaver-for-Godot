using Godot;

namespace AutoSaverPlugin.Shared;

public enum VerboseLevel
{
    OFF = 0,
    MIN = 1,
    MAX = 2,
    SECRET = 3
}

public enum LogType
{
    MINOR = 0,
    MAJOR = 1,
    WARN_ERR = 2,
    DEBUG = 3
}

public interface ILoggerService
{
    bool IsLogInfoEnable { get; }
    void SetOutput(VerboseLevel verboseLevel);
    void Log(string message, LogType logLevel);
    void LogDiagnostic(string message);
    void LogInfo(string message);
    void LogError(string message);
    void LogDebug(string message);
}

internal sealed class Logger : ILoggerService
{
    private VerboseLevel _configuredVerboseLevel = VerboseLevel.OFF;

    public VerboseLevel VerboseLevel => _configuredVerboseLevel;

    public bool IsLogInfoEnable => _configuredVerboseLevel >= VerboseLevel.MIN;

    public Logger() { }

    public void SetOutput(VerboseLevel verboseLevel)
    {
        _configuredVerboseLevel = verboseLevel;
    }

    // +----------------------+---------------+---------------------+---------------------+
    // | LogType\VerboseLevel |      MIN      |         MAX         |         SECRET      |
    // +----------------------+---------------+---------------------+---------------------+
    // | DEBUG                | -             |          -          | GD.Print("[DEBUG]") |
    // | MINOR                | -             | GD.Print("[INFO]")  | GD.Print("[INFO]")  |
    // | MAJOR                | GD.Print()    | GD.Print()          | GD.Print()          |
    // | ERROR                | GD.PrintErr() | GD.PrintErr()       | GD.PrintErr()       |
    // +----------------------+---------------+---------------------+---------------------+

    public void Log(string message, LogType logType)
    {
        if (_configuredVerboseLevel == VerboseLevel.OFF) return;

        string prefix = $"[{PluginInfo.NameShort}]";
        switch (logType)
        {
            case LogType.DEBUG:
                if (_configuredVerboseLevel == VerboseLevel.SECRET)
                    GD.Print($"{prefix}[DEBUG] {message}");
                break;
            case LogType.MINOR:
                if (_configuredVerboseLevel >= VerboseLevel.MAX)
                    GD.Print($"{prefix} {message}"); // [INFO]
                break;
            case LogType.MAJOR:
                GD.Print($"{prefix} {message}");
                break;
            case LogType.WARN_ERR:
                GD.PrintErr($"{prefix}[ERROR] {message}");
                break;
        }
    }

    public void LogDebug(string message) => Log(message, LogType.DEBUG);
    public void LogDiagnostic(string message) => Log(message, LogType.MINOR);
    public void LogInfo(string message) => Log(message, LogType.MAJOR);
    public void LogError(string message) => Log(message, LogType.WARN_ERR);
}
