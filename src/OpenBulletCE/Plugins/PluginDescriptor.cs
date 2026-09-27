using PluginFramework;
using PluginFramework.Attributes;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace OpenBulletCE.Plugins;

/// <summary>
/// Describes a loaded plugin — the view renders this dynamically
/// instead of using hardcoded UserControls.
/// </summary>
public class PluginDescriptor
{
    public string Name => PluginType.Name;
    public string AssemblyName => PluginType.Assembly.GetName().Name;
    public Type PluginType { get; set; }
    public IPlugin Plugin { get; set; }
    public List<PluginPropertyDescriptor> Properties { get; set; } = new();
    public List<PluginMethodDescriptor> Methods { get; set; } = new();
    public bool IsViewModel { get; set; }
}

public class PluginPropertyDescriptor : ViewModelBase
{
    public string PropertyName { get; set; }
    public string Label { get; set; }
    public string Tooltip { get; set; }
    public string InputType { get; set; }
    public PropertyInfo PropertyInfo { get; set; }
    public object Plugin { get; set; }

    private object _value;
    public object Value
    {
        get => _value;
        set
        {
            _value = value;
            OnPropertyChanged();
            // Sync back to the plugin
            try { PropertyInfo?.SetValue(Plugin, Convert.ChangeType(value, PropertyInfo.PropertyType)); }
            catch { }
        }
    }

    // For Dropdown
    public string[] Options { get; set; }
    // For Numeric
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    // For Text/TextMulti
    public bool ReadOnly { get; set; }
    // For FilePicker
    public string Filter { get; set; }
}

public class PluginMethodDescriptor
{
    public string ButtonText { get; set; }
    public string MethodName { get; set; }
    public MethodInfo MethodInfo { get; set; }
    public object Plugin { get; set; }

    public void Invoke()
    {
        try { MethodInfo?.Invoke(Plugin, null); }
        catch (Exception ex) { OB.Logger.LogError(Components.Unknown, $"Plugin method {MethodName} failed: {ex.Message}"); }
    }
}
