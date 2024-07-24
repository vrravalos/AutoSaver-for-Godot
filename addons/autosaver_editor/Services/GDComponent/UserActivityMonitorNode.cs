using System;
using AutoSaverPlugin.Shared;
using Godot;

namespace AutoSaverPlugin.Services.GDComponent;

internal partial class UserActivityMonitorNode : Node
{
    public event Action<string, float> UserActivityDetected;

    private bool _isMonitoring = false;
    private DateTime? _lastActivityTimeUtc;
    private readonly ILoggerService _logger = ServiceProvider.GetService<ILoggerService>();

    public bool IsMonitoring => _isMonitoring;
    public bool AnyUserActivityDetected => _lastActivityTimeUtc.HasValue;

    public UserActivityMonitorNode()
    {
    }

    public void StartMonitoring() => _isMonitoring = true;

    public void StopMonitoring() => _isMonitoring = false;

    public override void _Input(InputEvent @event)
    {
        if (!_isMonitoring || !(@event is InputEventMouseMotion || @event is InputEventKey)) return;

        _lastActivityTimeUtc = DateTime.UtcNow;

        string eventType = @event is InputEventMouseMotion ? "MouseMotion" : "Key";
        _logger.LogDebug($"{nameof(UserActivityMonitorNode)}: User activity detected: {eventType}, LastDetectedActivityTimeInMillisec = {LastDetectedActivityTimeInMillisec()}");

        UserActivityDetected?.Invoke(eventType, LastDetectedActivityTimeInMillisec());
    }

    internal float LastDetectedActivityTimeInMillisec()
    {
        return _lastActivityTimeUtc.HasValue
            ? (float)((DateTime.UtcNow - _lastActivityTimeUtc.Value).TotalMilliseconds)
            : 0;
    }

    public bool NoActivityTriggered(float thresholdMillisec)
    {
        _logger.LogDebug($"{nameof(UserActivityMonitorNode)}: Checking for no activity in the last {thresholdMillisec}ms. Last activity was {LastDetectedActivityTimeInMillisec()}ms ago.");

        return LastDetectedActivityTimeInMillisec() > thresholdMillisec;
    }
}
