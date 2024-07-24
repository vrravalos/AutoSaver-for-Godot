using System.Text;
using AutoSaverPlugin.Contracts;

using AutoSaverPlugin.Shared;
using Godot;

namespace AutoSaverPlugin.UI;

/// <summary>
/// Builds and manages the toggle button for the Auto Save feature in the top menu.
/// </summary>
public class AutoSaveToggleMenuBuilder
{
    private CheckButton _autoSaveToggleButton;
    private readonly IAutoSaveManager _autoSaveManager = ServiceProvider.GetService<IAutoSaveManager>();
    private readonly IConfigurationManager _configManager = ServiceProvider.GetService<IConfigurationManager>();

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoSaveToggleMenuBuilder"/> class.
    /// </summary>
    public AutoSaveToggleMenuBuilder()
    {
        Build();
    }

    /// <summary>
    /// Gets the Auto Save toggle button with configured settings and event handlers.
    /// </summary>
    public CheckButton AutoSaveToggleButton => Build();

    /// <summary>
    /// Creates and initializes the Auto Save toggle button for the top menu.
    /// </summary>
    /// <returns>The initialized Auto Save toggle button.</returns>
    private CheckButton Build()
    {
        if (_autoSaveToggleButton != null)
            return _autoSaveToggleButton;

        _autoSaveToggleButton = new CheckButton
        {
            Text = "Auto Save"
        };
        _autoSaveToggleButton.Toggled += HandleAutoSaveToggleChanged;
        _autoSaveToggleButton.SetPressedNoSignal(_configManager.IsAutoSaverEnabled);

        return _autoSaveToggleButton;
    }

    /// <summary>
    /// Detaches the toggle event subscriptions to prevent memory leaks.
    /// </summary>
    internal void DetachAutoSaveToggleEvents()
    {
        _autoSaveToggleButton.Toggled -= HandleAutoSaveToggleChanged;
    }

    /// <summary>
    /// Handles changes to the Auto Save toggle, updating settings and service state accordingly.
    /// </summary>
    /// <param name="toggledOn">Indicates whether the feature is being enabled or disabled.</param>
    private void HandleAutoSaveToggleChanged(bool toggledOn)
    {
        _configManager.SetAutoSaverEnabled(enabled: toggledOn);
        _configManager.SaveSettings();

        if (toggledOn)
        {
            _autoSaveManager.Reactivate();
        }
        else
        {
            _autoSaveManager.Deactivate();
        }
    }

    /// <summary>
    /// Updates the toggle state based on external settings invocation.
    /// </summary>
    /// <param name="trace">The trace builder for logging.</param>
    /// <param name="enabled">Indicates the new enabled state.</param>
    internal void UpdateToggleStateFromSettings(bool enabled)
    {
        _autoSaveToggleButton.SetPressedNoSignal(enabled);
    }
}
