using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System;
using System.Collections.Generic;

namespace OpenBulletCE;

public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Type> _mappings = new();

    static ViewLocator()
    {
        // Auto-map ViewModels to Views by convention:
        // OpenBulletCE.ViewModels.XxxViewModel -> OpenBulletCE.Views.XxxView
        var vmAssembly = typeof(ViewLocator).Assembly;
        var vmType = typeof(ViewModels.ViewModelBase);
        var vmNamespace = "OpenBulletCE.ViewModels";
        var viewNamespace = "OpenBulletCE.Views";

        foreach (var type in vmAssembly.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract) continue;
            if (type.Namespace?.StartsWith(vmNamespace) != true) continue;
            if (!type.Name.EndsWith("ViewModel")) continue;

            // Views mirror the VM namespace: ViewModels.Pages.X -> Views.Pages.X
            var viewNs = type.Namespace!.Replace("ViewModels", "Views");
            var baseName = type.Name[..^"ViewModel".Length];

            var candidates = new List<string> { $"{viewNs}.{baseName}View" };
            if (baseName.EndsWith("Page"))
                candidates.Add($"{viewNs}.{baseName[..^"Page".Length]}View");
            // legacy flat namespace fallback
            candidates.Add($"{viewNamespace}.{baseName}View");

            foreach (var candidate in candidates)
            {
                var viewType = vmAssembly.GetType(candidate);
                if (viewType != null)
                {
                    _mappings[type] = viewType;
                    break;
                }
            }
        }
    }

    public Control Build(object? param)
    {
        if (param == null)
            return new TextBlock { Text = "No DataContext" };

        var type = param.GetType();
        if (_mappings.TryGetValue(type, out var viewType))
        {
            var view = (Control)Activator.CreateInstance(viewType)!;
            view.DataContext = param;
            return view;
        }

        // Fallback: try direct name convention
        var viewName = $"OpenBulletCE.Views.{type.Name.Replace("ViewModel", "View")}";
        var fallback = type.Assembly.GetType(viewName);
        if (fallback != null)
        {
            _mappings[type] = fallback;
            var view = (Control)Activator.CreateInstance(fallback)!;
            view.DataContext = param;
            return view;
        }

        return new TextBlock { Text = $"View not found: {type.Name}" };
    }

    public bool Match(object? data)
    {
        return data is ViewModels.ViewModelBase;
    }
}
