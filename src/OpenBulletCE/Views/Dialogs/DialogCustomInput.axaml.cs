using Avalonia.Controls;
using Avalonia.Interactivity;
using RuriLib.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace OpenBulletCE.Views.Dialogs;

public class CustomInputAnswer
{
    public string VariableName { get; set; } = "";
    public string Value { get; set; } = "";
}

public partial class DialogCustomInput : Window
{
    public List<CustomInputAnswer> Answers { get; private set; }

    public DialogCustomInput() : this(Enumerable.Empty<CustomInputAnswer>()) { }

    public DialogCustomInput(IEnumerable<CustomInputAnswer> inputs, string description = "")
    {
        InitializeComponent();
        if (!string.IsNullOrEmpty(description))
            DescriptionText.Text = description;
        InputsPanel.ItemsSource = inputs.ToList();
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        Answers = InputsPanel.ItemsSource?.Cast<CustomInputAnswer>().ToList();
        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
