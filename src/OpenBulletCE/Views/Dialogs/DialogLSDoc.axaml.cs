using Avalonia.Controls;
using Avalonia.Input;
using System.Xml;

namespace OpenBulletCE.Views.Dialogs;

public partial class DialogLSDoc : Window
{
    private XmlNode _main, _currentSection, _currentItem;
    private XmlNodeList _sections, _items;

    public DialogLSDoc()
    {
        InitializeComponent();

        var doc = new XmlDocument();
        try { doc.Load("LSDoc.xml"); }
        catch
        {
            ContentDisplay.Text = "No documentation file found (LSDoc.xml)";
            return;
        }

        _main = doc.DocumentElement?.SelectSingleNode("/doc");
        if (_main == null) return;

        _sections = _main.ChildNodes;
        foreach (XmlNode s in _sections)
            SectionComboBox.Items.Add(s.Attributes["name"].Value);

        if (_sections.Count > 0)
        {
            SectionComboBox.SelectedIndex = 0;
            _currentSection = _sections[0];
            SwitchPage();
        }
    }

    private void Section_Changed(object? sender, SelectionChangedEventArgs e)
    {
        try
        {
            _currentSection = _sections.Item(SectionComboBox.SelectedIndex);
            SwitchPage();
        }
        catch { }
    }

    private void SwitchPage()
    {
        _items = _currentSection.ChildNodes;
        MenuPanel.Children.Clear();

        foreach (XmlNode i in _items)
        {
            var name = i.Attributes["name"].Value;
            var btn = new Button
            {
                Content = name,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                Margin = new Avalonia.Thickness(0, 1),
                Tag = name
            };
            btn.Click += MenuItem_Clicked;
            MenuPanel.Children.Add(btn);
        }
    }

    private void MenuItem_Clicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.Tag is string name)
            {
                foreach (XmlNode i in _items)
                {
                    if (i.Attributes["name"].Value == name)
                    {
                        _currentItem = i;
                        break;
                    }
                }
                DisplayContent();
            }
        }
        catch { }
    }

    private void DisplayContent()
    {
        TitleLabel.Text = _currentItem.Attributes["name"].Value;
        ContentDisplay.Text = _currentItem.InnerText;
    }
}
