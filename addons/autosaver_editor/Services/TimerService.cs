using System;
using AutoSaverPlugin.Contracts;
using AutoSaverPlugin.Shared;
using Godot;

namespace AutoSaverPlugin.Services
{
    internal sealed class TimerService : ITimerService
    {
        private Timer _timer;
        private AutoSaverEditorPlugin _plugin;
        private Action _timeoutAction;
        private ILoggerService _logger;

        public TimerService(ILoggerService loggerService)
        {
            _logger = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        }

        public ITimerService AttachTo(AutoSaverEditorPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            return this;
        }

        public ITimerService OnTimeout(Action action, bool oneShot = false)
        {
            _timeoutAction = action ?? throw new ArgumentNullException(nameof(action));
            SetupTimer(oneShot);
            return this;
        }

        public ITimerService Begin(float intervalSeconds)
        {
            if (_timer == null)
            {
                _logger.LogError("Timer is not initialized. Call OnTimeout first.");
                return this;
            }

            _timer.WaitTime = intervalSeconds;

            if (!_timer.IsInsideTree())
            {
                _plugin.AddChild(_timer);
            }

            _timer.Start();
            return this;
        }

        public ITimerService End()
        {
            DisposeTimer();
            return this;
        }

        private void SetupTimer(bool oneShot)
        {
            DisposeTimer();

            _timer = new Timer
            {
                OneShot = oneShot
            };

            _timer.Timeout += OnTimerTimeout;
        }

        private void OnTimerTimeout()
        {
            _timeoutAction?.Invoke();
        }

        private void DisposeTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                if (_timer.IsInsideTree())
                {
                    _plugin.RemoveChild(_timer);
                }

                _timer.Timeout -= OnTimerTimeout;
                _timer.QueueFree();
                _timer = null;
            }
        }
    }
}
