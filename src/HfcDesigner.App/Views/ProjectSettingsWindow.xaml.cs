using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HfcDesigner.Core.Formats;
using HfcDesigner.Core.Project;
using Microsoft.Win32;

namespace HfcDesigner.App.Views;

public partial class ProjectSettingsWindow : Window
{
    private readonly Dictionary<string, TextBox> _fields = new();

    // (settings property key, spec file kind, dialog filter) for every path
    // that "Set All Spec Files" fills in from one chosen .PAR file.
    private static readonly (string Key, SpecFileKind Kind)[] SpecFileFields =
    [
        ("ParametersFile", SpecFileKind.Parameters),
        ("ActivesFile", SpecFileKind.Actives),
        ("TapsFile", SpecFileKind.Taps),
        ("CouplersFile", SpecFileKind.Couplers),
        ("CablesFile", SpecFileKind.Cables),
        ("PricingFile", SpecFileKind.Pricing),
        ("PerformanceFile", SpecFileKind.Performance),
        ("MapGridFile", SpecFileKind.MapGrid),
    ];

    public ProjectSettings Result { get; private set; } = new();

    public ProjectSettingsWindow(ProjectSettings current)
    {
        InitializeComponent();
        BuildForm();
        LoadValues(current);
    }

    private void BuildForm()
    {
        AddSectionHeader("Folders");
        AddFolderRow("NetworkFolder", "Network Folder", "Path where you will save your network .NTW files");
        AddFolderRow("MiscFolder", "Misc Folder", "Miscellaneous files or Reports folders");
        AddFolderRow("ControlFileFolder", "Control File Folder", "");
        AddFolderRow("ReportFileFolder", "Report File Folder", "");

        AddSectionHeader("Spec Files");
        AddSetAllFilesRow();
        AddFileRow("ParametersFile", "Parameters File (.PAR)", "Lode Data Parameters Files (*.par)|*.par|All files (*.*)|*.*");
        AddFileRow("ActivesFile", "Actives File (.ATV)", "Lode Data Actives Files (*.atv)|*.atv|All files (*.*)|*.*");
        AddFileRow("TapsFile", "Taps File (.TAP)", "Lode Data Taps Files (*.tap)|*.tap|All files (*.*)|*.*");
        AddFileRow("CouplersFile", "Couplers File (.CPR)", "Lode Data Couplers Files (*.cpr)|*.cpr|All files (*.*)|*.*");
        AddFileRow("CablesFile", "Cables File (.CBL)", "Lode Data Cables Files (*.cbl)|*.cbl|All files (*.*)|*.*");
        AddFileRow("PricingFile", "Pricing File (.PRC)", "Lode Data Pricing Files (*.prc)|*.prc|All files (*.*)|*.*");
        AddFileRow("PerformanceFile", "Performance File (.PER)", "Lode Data Performance Files (*.per)|*.per|All files (*.*)|*.*");
        AddFileRow("MapGridFile", "Map Grid File (.MGD)", "Lode Data Map Grid Files (*.mgd)|*.mgd|All files (*.*)|*.*");
    }

    private void AddSectionHeader(string text)
    {
        FieldsPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 12, 0, 4),
        });
    }

    /// <summary>
    /// Mirrors the real Design Assistant's "Set All Files" button: if your
    /// spec files all share one base name (e.g. lode.par, lode.atv, ...),
    /// pick the .PAR file once and every other spec path is filled in by
    /// swapping the extension.
    /// </summary>
    private void AddSetAllFilesRow()
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };

        var button = new Button { Content = "Set All Files...", Width = 140 };
        button.Click += (_, _) => SetAllFiles();
        panel.Children.Add(button);

        panel.Children.Add(new TextBlock
        {
            Text = "  Pick one .PAR file and fill in the rest by swapping the extension.",
            Foreground = Brushes.Gray,
            FontSize = 11,
            VerticalAlignment = VerticalAlignment.Center,
        });

        FieldsPanel.Children.Add(panel);
    }

    private void SetAllFiles()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select the Parameters file to base all spec file paths on",
            Filter = "Lode Data Parameters Files (*.par)|*.par|All files (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) != true) return;

        var directory = Path.GetDirectoryName(dialog.FileName) ?? "";
        var baseName = Path.GetFileNameWithoutExtension(dialog.FileName);

        foreach (var (key, kind) in SpecFileFields)
        {
            var candidate = Path.Combine(directory, baseName + kind.ToExtension().ToLowerInvariant());
            _fields[key].Text = candidate;
        }
    }

    private void AddFolderRow(string key, string label, string hint)
    {
        AddRow(key, label, hint, () =>
        {
            var dialog = new OpenFolderDialog { Title = $"Select {label}" };
            if (_fields[key].Text.Length > 0 && Directory.Exists(_fields[key].Text))
            {
                dialog.InitialDirectory = _fields[key].Text;
            }
            return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
        });
    }

    private void AddFileRow(string key, string label, string filter)
    {
        AddRow(key, label, "", () =>
        {
            var dialog = new OpenFileDialog { Title = $"Select {label}", Filter = filter };
            var current = _fields[key].Text;
            var currentDir = current.Length > 0 ? Path.GetDirectoryName(current) : null;
            if (!string.IsNullOrEmpty(currentDir) && Directory.Exists(currentDir))
            {
                dialog.InitialDirectory = currentDir;
            }
            return dialog.ShowDialog(this) == true ? dialog.FileName : null;
        });
    }

    private void AddRow(string key, string label, string hint, Func<string?> browse)
    {
        FieldsPanel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 6, 0, 2) });

        if (!string.IsNullOrEmpty(hint))
        {
            FieldsPanel.Children.Add(new TextBlock
            {
                Text = hint,
                Foreground = Brushes.Gray,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 2),
            });
        }

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textBox = new TextBox { Margin = new Thickness(0, 0, 8, 0) };
        Grid.SetColumn(textBox, 0);
        row.Children.Add(textBox);

        var browseButton = new Button { Content = "Browse...", Width = 90 };
        Grid.SetColumn(browseButton, 1);
        browseButton.Click += (_, _) =>
        {
            var picked = browse();
            if (picked is not null) textBox.Text = picked;
        };
        row.Children.Add(browseButton);

        FieldsPanel.Children.Add(row);
        _fields[key] = textBox;
    }

    private void LoadValues(ProjectSettings current)
    {
        _fields["NetworkFolder"].Text = current.NetworkFolder;
        _fields["MiscFolder"].Text = current.MiscFolder;
        _fields["ControlFileFolder"].Text = current.ControlFileFolder;
        _fields["ReportFileFolder"].Text = current.ReportFileFolder;
        _fields["ParametersFile"].Text = current.ParametersFile;
        _fields["ActivesFile"].Text = current.ActivesFile;
        _fields["TapsFile"].Text = current.TapsFile;
        _fields["CouplersFile"].Text = current.CouplersFile;
        _fields["CablesFile"].Text = current.CablesFile;
        _fields["PricingFile"].Text = current.PricingFile;
        _fields["PerformanceFile"].Text = current.PerformanceFile;
        _fields["MapGridFile"].Text = current.MapGridFile;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Result = new ProjectSettings
        {
            NetworkFolder = _fields["NetworkFolder"].Text.Trim(),
            MiscFolder = _fields["MiscFolder"].Text.Trim(),
            ControlFileFolder = _fields["ControlFileFolder"].Text.Trim(),
            ReportFileFolder = _fields["ReportFileFolder"].Text.Trim(),
            ParametersFile = _fields["ParametersFile"].Text.Trim(),
            ActivesFile = _fields["ActivesFile"].Text.Trim(),
            TapsFile = _fields["TapsFile"].Text.Trim(),
            CouplersFile = _fields["CouplersFile"].Text.Trim(),
            CablesFile = _fields["CablesFile"].Text.Trim(),
            PricingFile = _fields["PricingFile"].Text.Trim(),
            PerformanceFile = _fields["PerformanceFile"].Text.Trim(),
            MapGridFile = _fields["MapGridFile"].Text.Trim(),
        };
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
