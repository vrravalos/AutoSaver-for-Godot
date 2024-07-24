using System.Text;

namespace AutoSaverPlugin.Contracts;

internal interface IAutoSaveManager
{
    void Initialize(AutoSaverEditorPlugin plugin);

    void Activate();

    void Reactivate();

    void Deactivate();
}
