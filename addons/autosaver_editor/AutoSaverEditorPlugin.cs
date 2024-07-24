#if TOOLS
using System.Text;
using AutoSaverPlugin.UI;
using AutoSaverPlugin.UI.GDComponent;
using AutoSaverPlugin.Contracts;
using AutoSaverPlugin.Shared;
using Godot;
using static AutoSaverPlugin.Shared.CommonUtils;
using System;
using System.Diagnostics;

[Tool]
public partial class AutoSaverEditorPlugin : EditorPlugin
{
    private IAutoSaveManager _autoSaveManager;
    private IConfigurationManager _configManager;

    private PanelSettingsControlNode _panelConfigNode;
    private AutoSaveToggleMenuBuilder _menuTopBuilder;
    private CheckButton _menuAutoSaveToggle;

    public override string _GetPluginName() => _configManager.PluginFullName;


    public override Texture2D _GetPluginIcon()
    {
        return ResourceLoader.Load<Texture2D>(_configManager.PluginIConResourcePath);
    }


    public override void _EnterTree()
    {
        InitializeDependencies();
        _autoSaveManager.Initialize(this);
        SetupAutoSaveToggle();
        SetupSettingsPanel();
    }

    public override void _ExitTree()
    {
        DetachEvents();
        CleanupUI();
    }

    private void InitializeDependencies()
    {
        ServiceProvider.Initialize();

        _autoSaveManager = ServiceProvider.GetService<IAutoSaveManager>();
        _configManager = ServiceProvider.GetService<IConfigurationManager>();
    }

    private void SetupSettingsPanel()
    {
        _panelConfigNode = new PanelSettingsControlNode();
        AddControlToDock(DockSlot.LeftUr, _panelConfigNode);
    }

    private void SetupAutoSaveToggle()
    {
        _menuTopBuilder = new AutoSaveToggleMenuBuilder();
        _menuAutoSaveToggle = _menuTopBuilder.AutoSaveToggleButton;
        AddControlToContainer(CustomControlContainer.Toolbar, _menuAutoSaveToggle);
        _configManager.AutoSaverStateChanged += _menuTopBuilder.UpdateToggleStateFromSettings;
    }

    private void DetachEvents()
    {
        _menuTopBuilder.DetachAutoSaveToggleEvents();
        _configManager.AutoSaverStateChanged -= _menuTopBuilder.UpdateToggleStateFromSettings;
    }

    private void CleanupUI()
    {
        _autoSaveManager.Deactivate();

        RemoveControlFromDocks(_panelConfigNode);
        _panelConfigNode?.QueueFree();

        RemoveControlFromContainer(CustomControlContainer.Toolbar, _menuAutoSaveToggle);
        _menuAutoSaveToggle?.Free();
    }
}
#endif //TOOLS
