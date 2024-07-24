using System;
using AutoSaverPlugin.Contracts;
using AutoSaverPlugin.Shared;
using Godot;

namespace AutoSaverPlugin.Services;

public sealed class ConfigurationManager : IConfigurationManager
{
    // default autosaver settings
    private const int AS_AUTOSAVER_INTERVAL = 60;

    private const VerboseLevel AS_VERBOSE = VerboseLevel.OFF;
    private const int AS_POSTPONE_TIME = 5;
    private const int AS_ACTIVITY_CHECK_WINDOW = 1;
    private const bool AS_AUTOSAVE_SCENE = true;
    private const bool AS_AUTOSAVE_GDSCRIPT = true;

    // default Godot editor settings
    private const bool GDEDITOR_SAVE_ON_FOCUS_LOSS_DEFAULT = false;

    private const int GDEDITOR_AUTOSAVE_INTERVAL_SECS_DEFAULT = 0;

    // defines the use or not of Godot editor settings
    private const bool USE_GDEDITOR_SAVE_ON_FOCUS_LOSS = false;

    private const bool USE_GDEDITOR_AUTOSAVE_INTERVAL_SECS = true;

    private readonly ILoggerService _logger;
    private readonly ConfigFile _configFile = new ConfigFile();
    private readonly string _configFilePath;
    private EditorSettings _gdEditorSettings => EditorInterface.Singleton.GetEditorSettings();

    private VerboseLevel _verboseLevel = AS_VERBOSE;

    public VerboseLevel VerboseLevelSetting
    {
        get => _verboseLevel;
        private set
        {
            _verboseLevel = value;
            _logger?.SetOutput(value);
        }
    }

    public int AutoSaverIntervalSetting { get; private set; } = AS_AUTOSAVER_INTERVAL;
    public int PostponeTimeSetting { get; private set; } = AS_POSTPONE_TIME;
    public int ActivityCheckWindowSetting { get; private set; } = AS_ACTIVITY_CHECK_WINDOW;
    public bool IsOptionSaveScenesEnabled { get; private set; } = AS_AUTOSAVE_SCENE;
    public bool IsOptionSaveScriptsEnabled { get; private set; } = AS_AUTOSAVE_GDSCRIPT;
    public bool IsAutoSaverEnabled { get; private set; } = true;
    public bool GDEditor_save_on_focus_loss { get; private set; } = GDEDITOR_SAVE_ON_FOCUS_LOSS_DEFAULT;
    public int GDEditor_autosave_interval_secs { get; private set; } = GDEDITOR_AUTOSAVE_INTERVAL_SECS_DEFAULT;

    public string PluginFullName => PluginInfo.FullName;
    public string PluginShortName => PluginInfo.NameShort;
    public string PluginVersion { get; } = CommonUtils.GetPluginVersion();

    public string PluginIConResourcePath => PluginInfo.BaseResourcePath + PluginInfo.PluginIcon;

    public bool HasGDEditorAutosaveEnabled => GDEditor_autosave_interval_secs > 0;

    public bool UseGDEditorSaveOnFocusLoss { get; private set; } = USE_GDEDITOR_SAVE_ON_FOCUS_LOSS;

    public bool UseGDEditorAutosaveIntervalSecs { get; private set; } = USE_GDEDITOR_AUTOSAVE_INTERVAL_SECS;

    public event Action<bool> AutoSaverStateChanged;

    public ConfigurationManager(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configFilePath = DetermineConfigFilePath();
    }

    public void SetSaverInterval(int seconds) => AutoSaverIntervalSetting = seconds;

    public void SetVerboseLevel(VerboseLevel level)
    {
        VerboseLevelSetting = level;
    }

    public void SetEditorSaveOnFocusLoss(bool enabled)
    {
        GDEditor_save_on_focus_loss = enabled;
        _gdEditorSettings.SetSetting("interface/editor/save_on_focus_loss", enabled);
    }

    public void SetEditorAutosaveIntervalSecs(int seconds)
    {
        GDEditor_autosave_interval_secs = seconds;
        _gdEditorSettings.SetSetting("text_editor/behavior/files/autosave_interval_secs", seconds);
    }

    public void SetSceneEnabled(bool enabled) => IsOptionSaveScenesEnabled = enabled;

    public void SetScriptEnabled(bool enabled) => IsOptionSaveScriptsEnabled = enabled;

    public void LoadSettings()
    {
        LoadFromGodotEditorSettings();

        LoadFromConfigFile();

        SyncGodotEditorSettings();
    }

    public void ResetSettings()
    {
        ResetGodotEditorSettings();

        AutoSaverIntervalSetting = AS_AUTOSAVER_INTERVAL;
        VerboseLevelSetting = AS_VERBOSE;
        IsAutoSaverEnabled = true;
        PostponeTimeSetting = AS_POSTPONE_TIME;
        ActivityCheckWindowSetting = AS_ACTIVITY_CHECK_WINDOW;
    }

    public void SaveSettings()
    {
        SaveToConfigFile();

        if (IsAutoSaverEnabled)
        {
            SyncGodotEditorSettings();
        }
        else
        {
            ResetGodotEditorSettings();
        }
    }

    private void SyncGodotEditorSettings()
    {
        _logger.LogDiagnostic($"Syncing Godot editor settings.. UseGDEditorAutosaveIntervalSecs={UseGDEditorAutosaveIntervalSecs}:{AutoSaverIntervalSetting}, SetEditorSaveOnFocusLoss={UseGDEditorSaveOnFocusLoss}");

        // update Godot editor settings from config file
        if (UseGDEditorAutosaveIntervalSecs)
            SetEditorAutosaveIntervalSecs(seconds: AutoSaverIntervalSetting);

        if (UseGDEditorSaveOnFocusLoss)
        {
            SetEditorSaveOnFocusLoss(enabled: IsAutoSaverEnabled);
        }
    }

    private void ResetGodotEditorSettings()
    {
        _logger.LogDiagnostic($"Resetting Godot editor settings.. UseGDEditorAutosaveIntervalSecs={UseGDEditorAutosaveIntervalSecs}, SetEditorSaveOnFocusLoss={UseGDEditorSaveOnFocusLoss}");

        // update Godot editor settings from default values
        if (UseGDEditorAutosaveIntervalSecs)
            SetEditorAutosaveIntervalSecs(seconds: GDEDITOR_AUTOSAVE_INTERVAL_SECS_DEFAULT);

        if (UseGDEditorSaveOnFocusLoss)
            SetEditorSaveOnFocusLoss(enabled: GDEDITOR_SAVE_ON_FOCUS_LOSS_DEFAULT);
    }

    private void LoadFromGodotEditorSettings()
    {
        GDEditor_autosave_interval_secs = (int)_gdEditorSettings.GetSetting("text_editor/behavior/files/autosave_interval_secs");
        GDEditor_save_on_focus_loss = (bool)_gdEditorSettings.GetSetting("interface/editor/save_on_focus_loss");
    }

    public void SetAutoSaverEnabled(bool enabled, bool noEmitSignal = false)
    {
        IsAutoSaverEnabled = enabled;

        SaveSettings();

        if (!noEmitSignal)
        {
            AutoSaverStateChanged?.Invoke(enabled);
        }
    }

    private string DetermineConfigFilePath()
    {
        var debugConfigPath = FindConfigFile(".debug.ini");
        if (!string.IsNullOrEmpty(debugConfigPath))
        {
            _logger.LogDiagnostic($"Using debug config file: {debugConfigPath}");
            return debugConfigPath;
        }

        var standardConfigPath = FindConfigFile(".ini");
        if (!string.IsNullOrEmpty(standardConfigPath))
        {
            _logger.LogDiagnostic($"Using standard config file: {standardConfigPath}");
            return standardConfigPath;
        }

        _logger.LogDiagnostic("Config file not found, using project settings.");
        return null;
    }

    private string FindConfigFile(string extension)
    {
        var fileName = PluginInfo.SettingsFileName.Replace(".ini", extension);
        return CommonUtils.GetAllProjectFiles(extension).Find(f => f.Contains(fileName));
    }

    private bool LoadFromConfigFile()
    {
        _logger.LogDiagnostic($"Loading config file: {_configFilePath}");
        Error error = _configFile.Load(_configFilePath);
        if (error != Error.Ok)
        {
            _logger.LogError($"Failed to load config file: {_configFilePath}, error: {error}");
            ResetSettings();
            return false;
        }

        AutoSaverIntervalSetting = (int)_configFile.GetValue(PluginInfo.RootSettings, PluginInfo.KeyIntervalSec, AS_AUTOSAVER_INTERVAL);

        VerboseLevelSetting = Enum.TryParse((string)_configFile.GetValue(PluginInfo.RootSettings, PluginInfo.KeyVerbose, AS_VERBOSE.ToString()),
                                                            out VerboseLevel result)
                                                                ? result
                                                                : AS_VERBOSE;

        IsAutoSaverEnabled = (bool)_configFile.GetValue(PluginInfo.RootSettings, PluginInfo.KeyEnabled, true);
        PostponeTimeSetting = (int)_configFile.GetValue(PluginInfo.RootSettings, PluginInfo.KeyPostponeTimeSec, AS_POSTPONE_TIME);
        ActivityCheckWindowSetting = (int)_configFile.GetValue(PluginInfo.RootSettings, PluginInfo.KeyActivityCheckWindowSec, AS_ACTIVITY_CHECK_WINDOW);
        IsOptionSaveScenesEnabled = (bool)_configFile.GetValue(PluginInfo.RootSettings, PluginInfo.KeyAutosaveScene, AS_AUTOSAVE_SCENE);
        IsOptionSaveScriptsEnabled = (bool)_configFile.GetValue(PluginInfo.RootSettings, PluginInfo.KeyAutosaveGDScript, AS_AUTOSAVE_GDSCRIPT);
        UseGDEditorAutosaveIntervalSecs = (bool)_configFile.GetValue(PluginInfo.RootSettings, PluginInfo.KeyUseGDEditorAutosaveInterval, USE_GDEDITOR_AUTOSAVE_INTERVAL_SECS);
        UseGDEditorSaveOnFocusLoss = (bool)_configFile.GetValue(PluginInfo.RootSettings, PluginInfo.KeyUseGDEditorSaveOnFocusLoss, USE_GDEDITOR_SAVE_ON_FOCUS_LOSS);

        return true;
    }

    private bool SaveToConfigFile()
    {
        _logger.LogDiagnostic($"Saving settings to config file: {_configFilePath}");
        _configFile.SetValue(PluginInfo.RootSettings, PluginInfo.KeyIntervalSec, AutoSaverIntervalSetting);
        _configFile.SetValue(PluginInfo.RootSettings, PluginInfo.KeyVerbose, VerboseLevelSetting.ToString());
        _configFile.SetValue(PluginInfo.RootSettings, PluginInfo.KeyEnabled, IsAutoSaverEnabled);
        _configFile.SetValue(PluginInfo.RootSettings, PluginInfo.KeyPostponeTimeSec, PostponeTimeSetting);
        _configFile.SetValue(PluginInfo.RootSettings, PluginInfo.KeyActivityCheckWindowSec, ActivityCheckWindowSetting);
        _configFile.SetValue(PluginInfo.RootSettings, PluginInfo.KeyAutosaveScene, IsOptionSaveScenesEnabled);
        _configFile.SetValue(PluginInfo.RootSettings, PluginInfo.KeyAutosaveGDScript, IsOptionSaveScriptsEnabled);
        _configFile.SetValue(PluginInfo.RootSettings, PluginInfo.KeyUseGDEditorAutosaveInterval, UseGDEditorAutosaveIntervalSecs);
        _configFile.SetValue(PluginInfo.RootSettings, PluginInfo.KeyUseGDEditorSaveOnFocusLoss, UseGDEditorSaveOnFocusLoss);

        var error = _configFile.Save(_configFilePath);
        if (error != Error.Ok)
        {
            _logger.LogError($"Failed to save config file: {_configFilePath}, error: {error}");
            return false;
        }
        return true;
    }
}
