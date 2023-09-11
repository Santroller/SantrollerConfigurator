using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using GuitarConfigurator.NetCore.Configuration.Inputs;
using GuitarConfigurator.NetCore.Configuration.Types;
using GuitarConfigurator.NetCore.ViewModels;

namespace GuitarConfigurator.NetCore.Configuration.Outputs.Combined;

public abstract class CombinedOutput : Output
{
    protected CombinedOutput(ConfigViewModel model) : base(model, new FixedInput(model, 0, false), Colors.Black,
        Colors.Black, Array.Empty<byte>(), false)
    {
    }

    public override string LedOnLabel => "";
    public override string LedOffLabel => "";

    public override bool IsCombined => true;
    public override bool IsStrum => false;
    public override bool IsKeyboard => false;

    public abstract void SetOutputsOrDefaults(IReadOnlyCollection<Output> outputs);

    public override string Generate(ConfigField mode, int debounceIndex, string extra,
        string combinedExtra,
        List<int> combinedDebounce, Dictionary<string, List<(int, Input)>> macros, BinaryWriter? writer)
    {
        return "";
    }
}