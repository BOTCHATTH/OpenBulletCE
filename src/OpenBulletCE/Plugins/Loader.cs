using PluginFramework;
using RuriLib;
using RuriLib.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenBulletCE.Plugins;

public static class Loader
{
    /// <summary>
    /// Loads plugins from a folder, returning descriptors the view can render.
    /// </summary>
    public static (List<PluginDescriptor>, List<IBlockPlugin>) LoadPlugins(string folder)
    {
        var plugins = new List<PluginDescriptor>();
        var blockPlugins = new List<IBlockPlugin>();

        if (!Directory.Exists(folder))
            return (plugins, blockPlugins);

        foreach (var dll in Directory.GetFiles(folder, "*.dll"))
        {
            Assembly asm;
            // Load from bytes so the dll is never file-locked (removable at runtime,
            // and Assembly.Location is empty under single-file publish anyway)
            try { asm = Assembly.Load(File.ReadAllBytes(dll)); }
            catch { continue; }

            // Hook dependency folder if it exists
            var depFolder = Path.Combine(folder, Path.GetFileNameWithoutExtension(dll));
            if (Directory.Exists(depFolder))
                Hook(new[] { depFolder });

            LoadDependencies(asm.GetReferencedAssemblies());

            foreach (var type in asm.GetTypes())
            {
                try
                {
                    if (type.GetInterface(nameof(IPlugin)) == typeof(IPlugin))
                    {
                        var descriptor = CreatePluginDescriptor(type);
                        if (descriptor != null)
                            plugins.Add(descriptor);
                    }
                    else if (type.GetInterface(nameof(IBlockPlugin)) == typeof(IBlockPlugin)
                        && type.GetTypeInfo().IsSubclassOf(typeof(BlockBase)))
                    {
                        blockPlugins.Add(Activator.CreateInstance(type) as IBlockPlugin);
                    }
                }
                catch { }
            }
        }

        return (plugins, blockPlugins);
    }

    private static PluginDescriptor CreatePluginDescriptor(Type type)
    {
        var isViewModel = type.GetTypeInfo().IsSubclassOf(typeof(ViewModelBase));

        var constructorParams = new object[] { };
        if (type.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(RuriLib.Interfaces.IApplication))))
        {
            constructorParams = new object[] { OB.App };
        }

        var plugin = Activator.CreateInstance(type, constructorParams) as IPlugin;
        if (plugin == null) return null;

        var descriptor = new PluginDescriptor
        {
            PluginType = type,
            Plugin = plugin,
            IsViewModel = isViewModel
        };

        foreach (var p in type.GetProperties().Where(p => Check.InputProperty(p)))
        {
            descriptor.Properties.Add(PluginBuilder.BuildPropertyDescriptor(plugin, p));
        }

        foreach (var m in type.GetMethods().Where(m => Check.Method(plugin, m)))
        {
            descriptor.Methods.Add(PluginBuilder.BuildMethodDescriptor(plugin, m));
        }

        return descriptor;
    }

    /// <summary>
    /// Runs a plugin method by name, syncing property values first.
    /// </summary>
    public static void RunMethod(PluginDescriptor descriptor, string methodName)
    {
        // Sync property values back to the plugin
        foreach (var prop in descriptor.Properties)
        {
            try
            {
                prop.PropertyInfo?.SetValue(descriptor.Plugin,
                    Convert.ChangeType(prop.Value, prop.PropertyInfo.PropertyType));
            }
            catch { }
        }

        var method = descriptor.Plugin.GetType().GetMethod(methodName);
        if (method == null) return;

        var parameters = method.GetParameters();
        var passed = new object[] { };

        if (parameters.Length == 1 && parameters.First().ParameterType == typeof(RuriLib.Interfaces.IApplication))
        {
            passed = new object[] { OB.App };
        }

        method.Invoke(descriptor.Plugin, passed);
    }

    public static void LoadDependencies(IEnumerable<AssemblyName> assemblies)
    {
        foreach (var asm in assemblies)
        {
            if (!AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().FullName == asm.FullName))
            {
                try
                {
                    AppDomain.CurrentDomain.Load(asm);
                    LoadDependencies(Assembly.Load(asm).GetReferencedAssemblies());
                }
                catch { }
            }
        }
    }

    public static void Hook(params string[] folders)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (loadedAssembly != null)
                return loadedAssembly;

            var n = new AssemblyName(args.Name);

            if (n.Name.EndsWith(".xmlserializers", StringComparison.OrdinalIgnoreCase))
                return null;

            if (n.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;

            string assy = null;
            foreach (var dir in folders)
            {
                assy = new[] { "*.dll", "*.exe" }.SelectMany(g => Directory.EnumerateFiles(dir, g)).FirstOrDefault(f =>
                {
                    try { return n.Name.Equals(AssemblyName.GetAssemblyName(f).Name, StringComparison.OrdinalIgnoreCase); }
                    catch (BadImageFormatException) { return false; }
                    catch (Exception ex) { throw new ApplicationException("Error loading assembly " + f, ex); }
                });

                if (assy != null)
                    return Assembly.LoadFrom(assy);
            }

            throw new ApplicationException("Assembly " + args.Name + " not found");
        };
    }
}
