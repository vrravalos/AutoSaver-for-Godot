using System;
using System.Collections.Generic;
using System.Linq;
using AutoSaverPlugin.Contracts;
using AutoSaverPlugin.Services;

namespace AutoSaverPlugin.Shared;

public static class ServiceProvider
{
    private static readonly Dictionary<Type, Func<object>> _services = new();
    private static readonly Dictionary<Type, object> _singletonInstances = new();

    public static void RegisterServiceAsTransient<TInterface, TImplementation>() where TImplementation : class, TInterface
    {
        _services[typeof(TInterface)] = () => CreateInstance<TImplementation>();
    }

    public static void RegisterServiceAsSingleton<TInterface, TImplementation>() where TImplementation : class, TInterface, new()
    {
        _services[typeof(TInterface)] = () => GetOrCreateSingletonInstance(typeof(TInterface), () => new TImplementation());
    }

    public static void RegisterServiceAsSingleton<TInterface>(Func<TInterface> factory)
    {
        _services[typeof(TInterface)] = () => GetOrCreateSingletonInstance(typeof(TInterface), () => factory());
    }

    public static T GetService<T>() where T : class
    {
        return (T)GetService(typeof(T));
    }

    private static object GetService(Type type)
    {
        if (_services.TryGetValue(type, out var serviceFactory))
        {
            return serviceFactory();
        }
        throw new InvalidOperationException($"Service of type {type} is not registered.");
    }

    private static T CreateInstance<T>() where T : class
    {
        var type = typeof(T);
        var constructor = type.GetConstructors()[0];
        var parameters = constructor.GetParameters();
        var parameterInstances = parameters.Select(p => GetService(p.ParameterType)).ToArray();
        return (T)constructor.Invoke(parameterInstances);
    }

    private static object GetOrCreateSingletonInstance(Type type, Func<object> factory)
    {
        if (!_singletonInstances.TryGetValue(type, out var instance))
        {
            instance = factory();
            _singletonInstances[type] = instance;
        }
        return instance;
    }

    public static void Initialize()
    {
        RegisterServiceAsSingleton<ILoggerService, Logger>();
        RegisterServiceAsSingleton<IConfigurationManager>(() => new ConfigurationManager(GetService<ILoggerService>()));
        RegisterServiceAsSingleton<ISceneStatusReporter>(() => new SceneTabStatusReporter(GetService<ILoggerService>()));
        RegisterServiceAsSingleton<IGDScriptStatusReporter>(() => new GDScriptStatusReporter(GetService<ILoggerService>()));

        RegisterServiceAsTransient<ITimerService, TimerService>();

        RegisterServiceAsSingleton<IAutoSaveManager>(() => new AutoSaveManager(
            GetService<ISceneStatusReporter>(),
            GetService<IGDScriptStatusReporter>(),
            GetService<IConfigurationManager>(),
            GetService<ILoggerService>(),
            GetService<ITimerService>(),
            GetService<ITimerService>()
        ));
    }
}
