using Avalonia.Controls;
using Avalonia.Interactivity;
using Extreme.Net;
using RuriLib;
using RuriLib.Models;
using System;
using System.Collections.Generic;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogAddProxies : Window
{
    public DialogAddProxies()
    {
        InitializeComponent();
    }

    private async void ImportFile_Click(object? sender, RoutedEventArgs e)
    {
        if (Alerter.Dialogs != null)
        {
            var files = await Alerter.Dialogs.OpenFilePickerAsync("Select Proxy File", "Text files", "txt", "csv", "lst");
            if (files.Length > 0)
            {
                var text = await System.IO.File.ReadAllTextAsync(files[0]);
                ProxiesTextBox.Text = (ProxiesTextBox.Text ?? "") + text;
            }
        }
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        var text = ProxiesTextBox.Text ?? "";
        if (string.IsNullOrWhiteSpace(text))
        {
            Close(false);
            return;
        }

        var type = (ProxyType)(TypeComboBox.SelectedIndex);
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var proxies = new List<CProxy>();

        foreach (var line in lines)
        {
            try
            {
                var p = new CProxy().Parse(line.Trim());
                p.Type = type;
                proxies.Add(p);
            }
            catch { }
        }

        if (proxies.Count > 0)
        {
            OB.ProxyManager.AddRange(proxies);
            OB.Logger.LogInfo(Components.ProxyManager, $"Added {proxies.Count} proxies");
        }

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
