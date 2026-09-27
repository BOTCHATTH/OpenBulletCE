using Avalonia.Controls;
using Avalonia.Layout;
using RuriLib;
using RuriLib.Functions.Conditions;
using Encoding = RuriLib.Functions.Conversions.Encoding;

namespace OpenBulletCE.Views.StackerBlocks;

public class PageBlockUtility : BlockPage
{
    private readonly BlockUtility _b;
    private readonly Grid _panels = new();

    // List sub-fields toggled by ListAction
    private Control _sepRow, _sortRow, _secondRow, _itemRow, _indexRow, _termRow;

    public PageBlockUtility(BlockUtility block) : base(block)
    {
        _b = block;

        // Group
        var grpRow = new DockPanel();
        grpRow.Children.Add(new TextBlock { Text = "Group:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) });
        grpRow.Children.Add(EnumCombo<UtilityGroup>(_b.Group, v => { _b.Group = v; UpdatePanels(); }));
        Panel.Children.Add(grpRow);

        var capRow = new DockPanel();
        capRow.Children.Add(Check("Is Capture", nameof(BlockUtility.IsCapture)));
        capRow.Children.Add(new TextBlock { Text = "  Var/Cap Name:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(10, 0, 5, 0) });
        capRow.Children.Add(Txt(nameof(BlockUtility.VariableName)));
        Panel.Children.Add(capRow);

        Panel.Children.Add(Row("Input String:", Txt(nameof(BlockUtility.InputString))));

        // LIST
        var list = VP();
        var laRow = new DockPanel();
        laRow.Children.Add(new TextBlock { Text = "List Action:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) });
        laRow.Children.Add(EnumCombo<ListAction>(_b.ListAction, v => { _b.ListAction = v; UpdateListFields(); }));
        list.Children.Add(laRow);
        list.Children.Add(Row("List Name:", Txt(nameof(BlockUtility.ListName))));
        _sepRow = Row("Separator:", Txt(nameof(BlockUtility.Separator)));
        list.Children.Add(_sepRow);
        _sortRow = Wrap(
            Check("Ascending", nameof(BlockUtility.Ascending)),
            Check("Numeric", nameof(BlockUtility.Numeric)));
        list.Children.Add(_sortRow);
        _secondRow = Row("Second List:", Txt(nameof(BlockUtility.SecondListName)));
        list.Children.Add(_secondRow);
        _itemRow = Row("Item:", Txt(nameof(BlockUtility.ListItem)));
        list.Children.Add(_itemRow);
        _indexRow = Row("Index:", Txt(nameof(BlockUtility.ListIndex)));
        list.Children.Add(_indexRow);
        _termRow = Wrap(
            Row("Comparer:", EnumCombo<Comparer>(_b.ListElementComparer, v => _b.ListElementComparer = v), 70),
            Row("Term:", Txt(nameof(BlockUtility.ListComparisonTerm)), 40));
        list.Children.Add(_termRow);
        _panels.Children.Add(list);

        // VARIABLE
        var var = VP();
        var varRow = new DockPanel();
        varRow.Children.Add(new TextBlock { Text = "Var Action:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) });
        varRow.Children.Add(EnumCombo<VarAction>(_b.VarAction, v => _b.VarAction = v));
        var.Children.Add(varRow);
        var.Children.Add(Row("Var Name:", Txt(nameof(BlockUtility.VarName))));
        var.Children.Add(Row("Split Separator:", Txt(nameof(BlockUtility.SplitSeparator))));
        _panels.Children.Add(var);

        // CONVERSION
        var conv = VP();
        conv.Children.Add(Row("Convert from:", EnumCombo<Encoding>(_b.ConversionFrom, v => _b.ConversionFrom = v), 90));
        conv.Children.Add(Row("Convert to:", EnumCombo<Encoding>(_b.ConversionTo, v => _b.ConversionTo = v), 90));
        _panels.Children.Add(conv);

        // FILE
        var file = VP();
        var fileRow = new DockPanel();
        fileRow.Children.Add(new TextBlock { Text = "File Action:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) });
        fileRow.Children.Add(EnumCombo<FileAction>(_b.FileAction, v => _b.FileAction = v));
        file.Children.Add(fileRow);
        file.Children.Add(Row("File Path:", Txt(nameof(BlockUtility.FilePath))));
        _panels.Children.Add(file);

        // FOLDER
        var folder = VP();
        var folderRow = new DockPanel();
        folderRow.Children.Add(new TextBlock { Text = "Folder Action:", VerticalAlignment = VerticalAlignment.Center, Margin = new Avalonia.Thickness(0, 0, 8, 0) });
        folderRow.Children.Add(EnumCombo<FolderAction>(_b.FolderAction, v => _b.FolderAction = v));
        folder.Children.Add(folderRow);
        folder.Children.Add(Row("Folder Path:", Txt(nameof(BlockUtility.FolderPath))));
        _panels.Children.Add(folder);

        Panel.Children.Add(_panels);

        UpdatePanels();
        UpdateListFields();
    }

    private static StackPanel VP() => new() { Spacing = 4 };

    private static StackPanel Wrap(params Control[] c)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        foreach (var x in c) p.Children.Add(x);
        return p;
    }

    private void UpdatePanels()
    {
        var idx = _b.Group switch
        {
            UtilityGroup.List => 0,
            UtilityGroup.Variable => 1,
            UtilityGroup.Conversion => 2,
            UtilityGroup.File => 3,
            UtilityGroup.Folder => 4,
            _ => -1
        };
        for (int i = 0; i < _panels.Children.Count; i++)
            _panels.Children[i].IsVisible = i == idx;
    }

    private void UpdateListFields()
    {
        _sepRow.IsVisible = _b.ListAction == ListAction.Join;
        _sortRow.IsVisible = _b.ListAction == ListAction.Sort;
        _secondRow.IsVisible = _b.ListAction is ListAction.Concat or ListAction.Zip or ListAction.Map;
        _itemRow.IsVisible = _b.ListAction == ListAction.Add;
        _indexRow.IsVisible = _b.ListAction == ListAction.Remove;
        _termRow.IsVisible = _b.ListAction == ListAction.RemoveValues;
    }
}
