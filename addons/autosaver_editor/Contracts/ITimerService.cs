using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSaverPlugin.Contracts;

public interface ITimerService
{
    ITimerService AttachTo(AutoSaverEditorPlugin pluginCaller);
    ITimerService OnTimeout(Action onAutosaveTimerTimeout, bool oneShot = false);
    ITimerService Begin(float intervalSec);
    ITimerService End();
}
