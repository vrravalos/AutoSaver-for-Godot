using System;
using AutoSaverPlugin.Contracts;
using AutoSaverPlugin.Shared;
using Godot;

namespace AutoSaverPlugin.UI.GDComponent;

[Tool]
public partial class PanelSettingsControlNode : Control
{
    private readonly IAutoSaveManager _autoSaveManager = ServiceProvider.GetService<IAutoSaveManager>();
    private readonly IConfigurationManager _configManager = ServiceProvider.GetService<IConfigurationManager>();
    private readonly ILoggerService _logger = ServiceProvider.GetService<ILoggerService>();

    private SpinBox _intervalSpinBox;
    private OptionButton _verboseLevelOption;
    private Button _saveButton;
    private CheckButton _enableToggle;
    private CheckBox _autosaveSceneCheckBox;
    private CheckBox _autosaveGDScriptCheckBox;
    private Label _lblStatus;

    public override void _Ready()
    {
        Name = _configManager.PluginFullName;

        SetupUI();
        LoadPanelSettings();
        _configManager.AutoSaverStateChanged += OnInvokeBySettings;
    }

    private void SetupUI()
    {
        var vbox = new VBoxContainer();
        AddChild(vbox);

        SetupEnableToggle(vbox);
        vbox.AddChild(new HSeparator());
        SetupIntervalSpinBox(vbox);

        if (_configManager.VerboseLevelSetting == VerboseLevel.SECRET)
        {
            SetupAutosaveSceneCheckBox(vbox);
            SetupAutosaveGDScriptCheckBox(vbox);
            SetupVerboseLevelOption(vbox);
        }

        SetupSaveButton(vbox);
        SetupStatusLabel(vbox);
        vbox.AddChild(new HSeparator());
        SetupFooter(vbox);
    }

    private void SetupAutosaveSceneCheckBox(VBoxContainer vbox)
    {
        var autosaveSceneHBox = new HBoxContainer();
        vbox.AddChild(autosaveSceneHBox);
        autosaveSceneHBox.AddChild(new Label { Text = "Autosave modified scenes" });
        _autosaveSceneCheckBox = new CheckBox();
        autosaveSceneHBox.AddChild(_autosaveSceneCheckBox);
    }

    private void SetupAutosaveGDScriptCheckBox(VBoxContainer vbox)
    {
        var autosaveGDScriptHBox = new HBoxContainer();
        vbox.AddChild(autosaveGDScriptHBox);
        autosaveGDScriptHBox.AddChild(new Label { Text = "Autosave modified script files" });
        _autosaveGDScriptCheckBox = new CheckBox();
        autosaveGDScriptHBox.AddChild(_autosaveGDScriptCheckBox);
    }

    private void SetupEnableToggle(VBoxContainer vbox)
    {
        var enableHBox = new HBoxContainer();
        vbox.AddChild(enableHBox);
        enableHBox.AddChild(new Label { Text = "Enable AutoSaver:" });
        _enableToggle = new CheckButton();
        enableHBox.AddChild(_enableToggle);
        _enableToggle.Toggled += OnPanelAutoSaveToggled;
    }

    private void SetupIntervalSpinBox(VBoxContainer vbox)
    {
        var intervalHBox = new HBoxContainer();
        vbox.AddChild(intervalHBox);
        intervalHBox.AddChild(new Label { Text = "Autosave interval (seconds):" });
        _intervalSpinBox = new SpinBox { MinValue = 5, MaxValue = 300, Value = 60, Step = 5 };
        intervalHBox.AddChild(_intervalSpinBox);
    }

    private void SetupVerboseLevelOption(VBoxContainer vbox)
    {
        var verboseHBox = new HBoxContainer();
        vbox.AddChild(verboseHBox);
        verboseHBox.AddChild(new Label { Text = "Verbose level:" });
        _verboseLevelOption = new OptionButton();

        _verboseLevelOption.AddItem("OFF", (int)VerboseLevel.OFF);
        _verboseLevelOption.AddItem("MIN", (int)VerboseLevel.MIN);
        _verboseLevelOption.AddItem("MAX", (int)VerboseLevel.MAX);
        _verboseLevelOption.AddItem("SECRET", (int)VerboseLevel.SECRET);

        verboseHBox.AddChild(_verboseLevelOption);
    }

    private void SetupSaveButton(VBoxContainer vbox)
    {
        _saveButton = new Button { Text = "Update Settings" };
        vbox.AddChild(_saveButton);
        _saveButton.Pressed += OnSaveButtonPressed;
    }

    private void SetupStatusLabel(VBoxContainer vbox)
    {
        _lblStatus = new Label();
        vbox.AddChild(_lblStatus);
        _lblStatus.Text = "Settings loaded @" + CommonUtils.GetCurrentTimestamp();
    }

    private void SetupFooter(VBoxContainer vbox)
    {
        string footerText = $"AutoSaver for Godot Editor v.{_configManager.PluginVersion} by Victor R. R. Avalos";
        vbox.AddChild(new LinkButton { Text = footerText, Uri = "https://github.com/vrravalos" });
    }

    private void OnInvokeBySettings(bool enabled)
    {
        _logger.LogDebug($"{{Panel: {nameof(OnInvokeBySettings)}}} AutoSaver enabled state changed..");
        _enableToggle.SetPressedNoSignal(enabled);
        UpdateUIState();
    }

    private void LoadPanelSettings()
    {
        _logger.LogDiagnostic("Loading panel settings..");

        _enableToggle.SetPressedNoSignal(_configManager.IsAutoSaverEnabled);
        _intervalSpinBox.SetValueNoSignal(_configManager.AutoSaverIntervalSetting);
        _verboseLevelOption?.Select((int)_configManager.VerboseLevelSetting);
        _autosaveSceneCheckBox?.SetPressedNoSignal(_configManager.IsOptionSaveScenesEnabled);
        _autosaveGDScriptCheckBox?.SetPressedNoSignal(_configManager.IsOptionSaveScriptsEnabled);
        UpdateUIState();
    }

    private void OnSaveButtonPressed()
    {
        _logger.LogDebug("Saving settings..");
        SaveSettings(noSignal: true);
        UpdateUIState();
    }

    public void OnPanelAutoSaveToggled(bool buttonPressed)
    {
        SaveSettings();
        UpdateUIState();
    }

    public override void _ExitTree()
    {
        _enableToggle.Toggled -= OnPanelAutoSaveToggled;
        _configManager.AutoSaverStateChanged -= OnInvokeBySettings;
    }

    private void UpdateUIState()
    {
        bool isEnabled = _enableToggle.ButtonPressed;
        _intervalSpinBox.Editable = isEnabled;
        if (_verboseLevelOption != null)
            _verboseLevelOption.Disabled = !isEnabled;

        if (_autosaveSceneCheckBox != null)
            _autosaveSceneCheckBox.Disabled = !isEnabled;

        if (_autosaveGDScriptCheckBox != null)
            _autosaveGDScriptCheckBox.Disabled = !isEnabled;

        _saveButton.Disabled = !isEnabled;
        _lblStatus.Text = "Settings updated @" + CommonUtils.GetCurrentTimestamp();
    }

    private void SaveSettings(bool noSignal = false)
    {
        SaveSettings(noEmitSignal: noSignal,
                      interval: (int?)_intervalSpinBox.Value,
                      verboseLevel: _verboseLevelOption?.Selected != null ? (VerboseLevel?)_verboseLevelOption.Selected : null,
                      sceneEnabled: _autosaveSceneCheckBox?.ButtonPressed,
                      scriptEnabled: _autosaveGDScriptCheckBox?.ButtonPressed,
                      autoSaverEnabled: _enableToggle.ButtonPressed);
    }

    private void SaveSettings(bool noEmitSignal, int? interval = null,
                              VerboseLevel? verboseLevel = null, bool? sceneEnabled = null, bool? scriptEnabled = null,
                              bool? autoSaverEnabled = null)
    {
        if (interval.HasValue)
        {
            _configManager.SetSaverInterval(interval.Value);
        }

        if (verboseLevel.HasValue)
        {
            _configManager.SetVerboseLevel(verboseLevel.Value);
        }

        if (sceneEnabled.HasValue)
        {
            _configManager.SetSceneEnabled(enabled: sceneEnabled.Value);
        }

        if (scriptEnabled.HasValue)
        {
            _configManager.SetScriptEnabled(enabled: scriptEnabled.Value);
        }

        if (autoSaverEnabled.HasValue)
        {
            _configManager.SetAutoSaverEnabled(enabled: autoSaverEnabled.Value, noEmitSignal: noEmitSignal);
        }

        _configManager.SaveSettings();

        if (_configManager.IsAutoSaverEnabled)
        {
            _autoSaveManager.Reactivate();
        }
        else
        {
            _autoSaveManager.Deactivate();
        }
    }
}
