using PluginFramework;
using PluginFramework.Attributes;
using RuriLib.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OpenBulletCE.Plugins;

public static class Check
{
    private static Dictionary<Type, Type> _requiredPropertyTypes = new Dictionary<Type, Type>()
    {
        { typeof(InfoText), typeof(string) },
        { typeof(Text), typeof(string) },
        { typeof(Numeric), typeof(int) },
        { typeof(Checkbox), typeof(bool) },
        { typeof(TextMulti), typeof(string[]) },
        { typeof(FilePicker), typeof(string) },
        { typeof(Dropdown), typeof(string) },
        { typeof(WordlistPicker), typeof(RuriLib.Models.Wordlist) },
        { typeof(ConfigPicker), typeof(RuriLib.ViewModels.ConfigViewModel) }
    };

    public static bool InputProperty(PropertyInfo property)
    {
        if (property.GetCustomAttributes().Count(a => a is InputField) != 1)
            return false;

        var attribute = property.GetCustomAttribute<InputField>();

        if (!_requiredPropertyTypes.ContainsKey(attribute.GetType()))
            throw new Exception($"Unknown attribute type {attribute.GetType()}");

        var requiredType = _requiredPropertyTypes[attribute.GetType()];

        if (property.PropertyType != requiredType)
            throw new Exception($"The property {property.Name} must be of type {requiredType}");

        return true;
    }

    public static bool Method(IPlugin plugin, MethodInfo method)
    {
        if (method.GetCustomAttributes().Count(a => a is Button) != 1)
            return false;

        var parameters = method.GetParameters();

        if (parameters.Length > 1)
            return false;

        if (parameters.Length == 1 && !parameters.Any(p => p.ParameterType == typeof(IApplication)))
            return false;

        return true;
    }
}
