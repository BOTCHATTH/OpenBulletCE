using PluginFramework;
using PluginFramework.Attributes;
using RuriLib.ViewModels;
using System;
using System.Reflection;

namespace OpenBulletCE.Plugins;

/// <summary>
/// Replaces the WPF Build.cs — creates property descriptors
/// instead of hardcoded UserControls.
/// </summary>
public static class PluginBuilder
{
    public static PluginPropertyDescriptor BuildPropertyDescriptor(object plugin, PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<InputField>();
        var descriptor = new PluginPropertyDescriptor
        {
            PropertyName = property.Name,
            PropertyInfo = property,
            Plugin = plugin,
            Label = attribute.label,
            Tooltip = attribute.tooltip
        };

        var defaultValue = property.GetValue(plugin);

        switch (attribute)
        {
            case InfoText:
                descriptor.InputType = "InfoText";
                descriptor.Value = defaultValue?.ToString() ?? "";
                break;
            case Text a:
                descriptor.InputType = "Text";
                descriptor.ReadOnly = a.readOnly;
                descriptor.Value = defaultValue?.ToString() ?? "";
                break;
            case Numeric a:
                descriptor.InputType = "Numeric";
                descriptor.MinValue = a.minimum;
                descriptor.MaxValue = a.maximum;
                descriptor.Value = defaultValue;
                break;
            case Checkbox:
                descriptor.InputType = "Checkbox";
                descriptor.Value = defaultValue;
                break;
            case TextMulti a:
                descriptor.InputType = "TextMulti";
                descriptor.ReadOnly = a.readOnly;
                descriptor.Value = defaultValue is string[] arr ? string.Join("\n", arr) : "";
                break;
            case FilePicker a:
                descriptor.InputType = "FilePicker";
                descriptor.Filter = a.filter;
                descriptor.Value = defaultValue?.ToString() ?? "";
                break;
            case Dropdown a:
                descriptor.InputType = "Dropdown";
                descriptor.Options = a.options;
                descriptor.Value = defaultValue?.ToString() ?? "";
                break;
            case WordlistPicker:
                descriptor.InputType = "WordlistPicker";
                descriptor.Value = defaultValue;
                break;
            case ConfigPicker:
                descriptor.InputType = "ConfigPicker";
                descriptor.Value = defaultValue;
                break;
            default:
                throw new NotImplementedException($"Unknown input type {attribute.GetType().Name}");
        }

        return descriptor;
    }

    public static PluginMethodDescriptor BuildMethodDescriptor(object plugin, MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<Button>();
        return new PluginMethodDescriptor
        {
            ButtonText = attribute.text,
            MethodName = method.Name,
            MethodInfo = method,
            Plugin = plugin
        };
    }
}
