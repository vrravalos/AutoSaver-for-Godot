using System.Collections.Generic;
using Godot;

namespace AutoSaverPlugin.Contracts;

internal interface IStatusReporter
{
    List<string> FetchModifiedItems();
}

internal interface ISceneStatusReporter : IStatusReporter
{ }

internal interface IGDScriptStatusReporter : IStatusReporter
{ }

