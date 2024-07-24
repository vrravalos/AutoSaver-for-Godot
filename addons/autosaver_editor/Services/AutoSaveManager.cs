using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoSaverPlugin.Contracts;
using AutoSaverPlugin.Services.GDComponent;
using AutoSaverPlugin.Shared;
using Godot;
using static AutoSaverPlugin.Shared.CommonUtils;

namespace AutoSaverPlugin.Services;

internal sealed class AutoSaveManager : IAutoSaveManager
{
    private readonly EditorInterface _editorInterface = EditorInterface.Singleton;
    private ScriptEditor _scriptEditor => _editorInterface.GetScriptEditor();
    private readonly ISceneStatusReporter _sceneReporter;
    private readonly IGDScriptStatusReporter _gdScriptReporter;
    private readonly ITimerService _timerAutoSaver;
    private readonly ITimerService _timerActivityUserCheck;
    private readonly IConfigurationManager _configManager;
    private readonly ILoggerService _logger;
    private AutoSaverEditorPlugin _plugin;
    private UserActivityMonitorNode _activityMonitor;

    public AutoSaveManager(ISceneStatusReporter sceneStatusReporter, IGDScriptStatusReporter scriptStatusReporter, IConfigurationManager configManager,
                           ILoggerService loggerService, ITimerService timerAutoSaver, ITimerService timerActivity)
    {
        _sceneReporter = sceneStatusReporter ?? throw new ArgumentNullException(nameof(sceneStatusReporter));
        _gdScriptReporter = scriptStatusReporter ?? throw new ArgumentNullException(nameof(scriptStatusReporter));
        _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        _logger = loggerService ?? throw new ArgumentNullException(nameof(loggerService));
        _timerAutoSaver = timerAutoSaver ?? throw new ArgumentNullException(nameof(timerAutoSaver));
        _timerActivityUserCheck = timerActivity ?? throw new ArgumentNullException(nameof(timerActivity));
    }

    public void Initialize(AutoSaverEditorPlugin plugin)
    {
        LoadingConfiguration();
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        Activate();
    }

    private void LoadingConfiguration()
    {
        _configManager.LoadSettings();
    }

    public void Activate() => SetupAutoSave(restart: false);

    public void Reactivate() => SetupAutoSave(restart: true);

    public void Deactivate()
    {
        _logger.LogDiagnostic("Stopping autosaver service..");
        _timerAutoSaver.End();
        _timerActivityUserCheck.End();
        RemoveActivityMonitor();
        PrintStatus();
    }

    private void SetupAutoSave(bool restart)
    {
        _logger.LogDiagnostic($"{(restart ? "Restarting" : "Initializing")} autosaver service..");
        _configManager.LoadSettings();
        SetTimers();
        ManageActivityMonitor(add: true);
        PrintStatus();
    }

    private void SetTimers()
    {
        _logger.LogDebug("Setting timers..");

        int intervalSec = _configManager.AutoSaverIntervalSetting;
        int timeToStartCheck = Math.Clamp(intervalSec - _configManager.ActivityCheckWindowSetting, 1, intervalSec);

        _timerAutoSaver.End().AttachTo(_plugin).OnTimeout(OnAutosaveTimerTimeout);
        _timerActivityUserCheck.End().AttachTo(_plugin).OnTimeout(StartMonitoringUserActivity, oneShot: true);

        if (_configManager.IsAutoSaverEnabled)
        {
            _timerAutoSaver.Begin(intervalSec);
            _timerActivityUserCheck.Begin(timeToStartCheck);
        }
    }

    private void ManageActivityMonitor(bool add)
    {
        if (add)
        {
            _activityMonitor = new UserActivityMonitorNode();
            _activityMonitor.UserActivityDetected += OnUserActivityDetected;
            _plugin.AddChild(_activityMonitor);
        }
        else
        {
            if (_activityMonitor != null)
            {
                _activityMonitor.UserActivityDetected -= OnUserActivityDetected;

                if (_activityMonitor.IsInsideTree())
                    _plugin?.RemoveChild(_activityMonitor);

                if (!_activityMonitor.IsQueuedForDeletion())
                    _activityMonitor.QueueFree();

                _activityMonitor = null;
            }
        }
    }

    private void RemoveActivityMonitor() => ManageActivityMonitor(add: false);

    private void OnUserActivityDetected(string eventName, float lastActivityTimeSec) =>
        _logger.LogDebug($"User activity detected: {eventName} at last activity time: {lastActivityTimeSec}ms");

    private void StartMonitoringUserActivity()
    {
        _logger.LogDebug($"Starting user activity monitoring..@{GetCurrentTimestamp()}");
        _activityMonitor.StartMonitoring();
    }

    private void OnAutosaveTimerTimeout()
    {
        _logger.LogDiagnostic($"Running autosaver @{GetCurrentTimestamp()}");
        PerformAutoSaveIfNeeded();
    }

    private void PerformAutoSaveIfNeeded()
    {
        const float inTheLastMilliSecs = 500f; // 0.5 sec
        if (!_activityMonitor.IsMonitoring || _activityMonitor.NoActivityTriggered(thresholdMillisec: inTheLastMilliSecs))
        {
            PerformAutoSave();
        }
        else
        {
            PostponeAutoSave();
        }
    }

    private void PerformAutoSave()
    {
        _activityMonitor.StopMonitoring();

        var modifiedScenes = _configManager.IsOptionSaveScenesEnabled ? GetModifiedItems(_sceneReporter.FetchModifiedItems()) : new List<string>();
        var modifiedScripts = _configManager.IsOptionSaveScriptsEnabled ? GetModifiedItems(_gdScriptReporter.FetchModifiedItems()) : new List<string>();

        if (modifiedScenes.Count == 0 && modifiedScripts.Count == 0)
        {
            _logger.LogDiagnostic("No modified items. Skipping autosave.");
            return;
        }

        // save all scenes (also save all scripts)
        bool savedAll = SaveScenes(modifiedScenes);

        if (!savedAll && !_configManager.HasGDEditorAutosaveEnabled)
            SaveFiles(modifiedScripts);

        LogAutosaveResult(modifiedScenes.Count + modifiedScripts.Count, modifiedScenes.Concat(modifiedScripts).ToList());
        SetTimers(); // Restart timers
    }

    private bool SaveScenes(List<string> modifiedScenes)
    {
        List<string> savedFiles = new();
        var openScenes = _editorInterface.GetOpenScenes();
        var editedScene = _editorInterface.GetEditedSceneRoot();
        var fnSceneRoot = Path.GetFileName(editedScene.SceneFilePath).Split('.')[0];
        bool saveAllAtOnce = false;
        int numFilesSaved = 0;

        foreach (string scenePath in openScenes)
        {
            var fileNameScenePath = Path.GetFileName(scenePath).Split('.')[0];
            if (modifiedScenes.Contains(fileNameScenePath))
            {
                numFilesSaved++;
                savedFiles.Add(scenePath);
                saveAllAtOnce = saveAllAtOnce || fnSceneRoot != fileNameScenePath;
            }
        }

        if (saveAllAtOnce)
        {
            _editorInterface.SaveAllScenes();
        }
        else if (numFilesSaved == 1)
        {
            var err = _editorInterface.SaveScene();
            if (err != Error.Ok)
            {
                _logger.LogError($"Failed to autoSave scene: {fnSceneRoot}. Error: {err}");
            }
        }

        return saveAllAtOnce;
    }

    private void SaveFiles(List<string> modifiedFiles)
    {
        if (modifiedFiles.Count > 0)
            _editorInterface.SaveAllScenes();
    }

    private void PostponeAutoSave()
    {
        _logger.LogDebug($"Postponing autoSave for {_configManager.PostponeTimeSetting}sec..");
        _timerAutoSaver.End().OnTimeout(OnAutosaveTimerTimeout).Begin(_configManager.PostponeTimeSetting);
    }

    private static List<string> GetModifiedItems(List<string> items)
    {
        var modifiedScripts = new List<string>();

        foreach (var i in items)
        {
            if (i.Contains("*"))
            {
                modifiedScripts.Add(i.Replace("(*)", ""));
            }
        }

        return modifiedScripts;
    }

    private void LogAutosaveResult(int numFilesSaved, List<string> savedFiles)
    {
        string currentTimestamp = GetCurrentTimestamp();
        if (numFilesSaved > 0)
        {
            _logger.LogInfo($"Autosave executed at {currentTimestamp}. {numFilesSaved} file(s) saved:");
            foreach (var file in savedFiles)
            {
                _logger.LogInfo($"- {file}");
            }
        }
        else
        {
            _logger.LogDiagnostic($"Autosave completed at {currentTimestamp}: No files saved.");
        }
    }

    private void PrintStatus()
    {
        string timestamp = GetCurrentTimestamp();
        string pluginSetTimestamp = $"Plugin set @{timestamp}.";
        string autosaveScene = $"scenes ({(_configManager.IsOptionSaveScenesEnabled ? "ON" : "OFF")})";
        string autosaveScript = $"GDScript files ({(_configManager.IsOptionSaveScriptsEnabled ? "ON" : "OFF")})";
        string verboseLevelMessage = $"Verbose level: {_configManager.VerboseLevelSetting}.";
        string editorMessage = $"[Editor] Autosave Interval: {_configManager.GDEditor_autosave_interval_secs}sec, [Editor] Save on focus loss: {_configManager.GDEditor_save_on_focus_loss}";

        string statusAutosaving = _configManager.IsAutoSaverEnabled ? "Autosaving every {_autoSaveConfig.AutoSaverIntervalSetting} seconds: {autosaveScene} and {autosaveScript}." : "Autosaving disabled.";

        _logger.LogInfo($"{pluginSetTimestamp} {statusAutosaving}");
        _logger.LogDiagnostic($"{verboseLevelMessage} {editorMessage}");
    }
}
