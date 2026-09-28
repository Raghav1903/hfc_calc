using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using HfcDesigner.App.Views;
using HfcDesigner.Core.Formats;
using HfcDesigner.Core.Project;
using Microsoft.Win32;

namespace HfcDesigner.App;

public partial class MainWindow : Window
{
    private readonly ProjectSettingsService _settingsService = new();
    private ProjectSettings _settings = new();
    private string? _currentProjectFilePath;

    public ObservableCollection<SpecFileListItem> SpecFileItems { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        SpecFileList.ItemsSource = SpecFileItems;
        LoadLastProjectIfAny();
    }

    private void LoadLastProjectIfAny()
    {
        var recents = _settingsService.LoadRecentProjects();
        RebuildRecentMenu(recents);

        var existing = recents.FirstOrDefault(File.Exists);
        if (existing is not null)
        {
            var loaded = _settingsService.Load(existing);
            if (loaded is not null)
            {
                _settings = loaded;
                _currentProjectFilePath = existing;
            }
        }

        RefreshSpecFileList();
        UpdateStatus();
    }

    private void RebuildRecentMenu(List<string> recents)
    {
        RecentProjectsMenu.Items.Clear();
        if (recents.Count == 0)
        {
            RecentProjectsMenu.Items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
            return;
        }

        foreach (var path in recents)
        {
            var item = new MenuItem { Header = path, Tag = path };
            item.Click += (_, _) => OpenProjectFile(path);
            RecentProjectsMenu.Items.Add(item);
        }
    }

    private void RefreshSpecFileList()
    {
        SpecFileItems.Clear();
        foreach (var (kind, path) in _settings.EnumerateSpecFiles())
        {
            SpecFileItems.Add(SpecFileListItem.FromPath(kind, path));
        }
    }

    private void UpdateStatus()
    {
        StatusText.Text = _currentProjectFilePath is null
            ? "No project loaded. Use File > Project Settings to configure spec file paths."
            : $"Project: {_currentProjectFilePath}";
    }

    private void ProjectSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ProjectSettingsWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        _settings = dialog.Result;

        var savePath = _currentProjectFilePath ?? PromptForProjectSavePath();
        if (savePath is null) return;

        _settingsService.Save(_settings, savePath);
        _currentProjectFilePath = savePath;
        RebuildRecentMenu(_settingsService.LoadRecentProjects());
        RefreshSpecFileList();
        UpdateStatus();
    }

    private string? PromptForProjectSavePath()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Project Settings",
            Filter = $"HFC Designer Project (*{ProjectSettingsService.ProjectFileExtension})|*{ProjectSettingsService.ProjectFileExtension}",
            FileName = "project" + ProjectSettingsService.ProjectFileExtension,
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private void OpenNetwork_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Network File",
            Filter = "Lode Data Network Files (*.ntw)|*.ntw|All files (*.*)|*.*",
        };
        if (Directory.Exists(_settings.NetworkFolder))
        {
            dialog.InitialDirectory = _settings.NetworkFolder;
        }

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var doc = SpecFileReader.Open(dialog.FileName, SpecFileKind.Network);
            ShowNetworkDocument(doc);
            MainTabs.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open '{dialog.FileName}':\n{ex.Message}", "Open Network File",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowNetworkDocument(SpecFileDocument doc)
    {
        NetworkTitleText.Text = string.IsNullOrEmpty(doc.Header.Title) ? "(untitled)" : doc.Header.Title;

        var version = doc.Header.DesignVersion is null ? "" : $" — {doc.Header.DesignVersion}";
        var decryptionNote = doc.BodyWasDecrypted
            ? $"Body de-obfuscated with a recovered {doc.DecryptionKeyPeriod}-byte repeating XOR key."
            : "Body did not match the known repeating-XOR obfuscation pattern — showing raw bytes as-is.";

        NetworkDetailsText.Text =
            $"File: {doc.FilePath}\n" +
            $"Code: {doc.Header.Code}{version}\n" +
            $"User/Node: {doc.Header.UserName}\n" +
            $"Size: {doc.FileSizeBytes:N0} bytes\n" +
            $"{decryptionNote}\n\n" +
            "Full schematic parsing (nodes, branches, cable spans, placed equipment) is not " +
            "implemented yet — the actual node/branch record layout hasn't been decoded. This view " +
            "shows the file header and any embedded text found in the de-obfuscated body. See " +
            "docs/FILE_FORMAT_NOTES.md for what's been found so far and what it would take to finish it.";

        NetworkTokensList.ItemsSource = doc.RawTokens
            .Select(t => new RawTokenRow(t.Offset, t.Text))
            .ToList();
    }

    private void OpenProjectFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Project Settings",
            Filter = $"HFC Designer Project (*{ProjectSettingsService.ProjectFileExtension})|*{ProjectSettingsService.ProjectFileExtension}|All files (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) == true)
        {
            OpenProjectFile(dialog.FileName);
        }
    }

    private void OpenProjectFile(string path)
    {
        var loaded = _settingsService.Load(path);
        if (loaded is null)
        {
            MessageBox.Show(this, $"Could not read project settings from '{path}'.", "Open Project",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _settings = loaded;
        _currentProjectFilePath = path;
        _settingsService.Save(_settings, path); // touches the recent-projects list
        RebuildRecentMenu(_settingsService.LoadRecentProjects());
        RefreshSpecFileList();
        UpdateStatus();
    }

    private void ReloadSpecFiles_Click(object sender, RoutedEventArgs e) => RefreshSpecFileList();

    private void SpecFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SpecFileList.SelectedItem is SpecFileListItem item)
        {
            LoadAndShowSpecFile(item);
        }
    }

    private void LoadAndShowSpecFile(SpecFileListItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Path))
        {
            item.Status = "Not configured";
            RecordsHeaderText.Text = $"{item.Kind.DisplayName()} — no path configured";
            RecordsGrid.ItemsSource = null;
            return;
        }

        if (!File.Exists(item.Path))
        {
            item.Status = "File not found";
            RecordsHeaderText.Text = $"{item.Kind.DisplayName()} — file not found: {item.Path}";
            RecordsGrid.ItemsSource = null;
            return;
        }

        try
        {
            var doc = item.Document ??= SpecFileReader.Open(item.Path, item.Kind);

            if (doc.IsStructured)
            {
                item.Status = $"Loaded ({doc.Records.Count} records)";
                RecordsHeaderText.Text = $"{item.Kind.DisplayName()} — {item.Path} — {doc.Records.Count} records";
                RecordsGrid.ItemsSource = doc.Records
                    .Select(r => new SpecRecordRow(r.Index, r.Offset, r.Name, r.Length))
                    .ToList();
            }
            else
            {
                item.Status = $"Loaded ({doc.RawTokens.Count} text entries, raw)";
                RecordsHeaderText.Text =
                    $"{item.Kind.DisplayName()} — {item.Path} — {doc.RawTokens.Count} text entries " +
                    "(structured record layout not yet reverse-engineered for this format)";
                RecordsGrid.ItemsSource = doc.RawTokens
                    .Select((t, i) => new SpecRecordRow(i, t.Offset, t.Text, t.Text.Length))
                    .ToList();
            }

            MainTabs.SelectedIndex = 1;
        }
        catch (Exception ex)
        {
            item.Status = "Error";
            RecordsHeaderText.Text = $"{item.Kind.DisplayName()} — failed to read: {ex.Message}";
            RecordsGrid.ItemsSource = null;
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}

public sealed class SpecFileListItem : INotifyPropertyChanged
{
    private string _status = "";

    public required SpecFileKind Kind { get; init; }
    public required string Path { get; init; }
    public SpecFileDocument? Document { get; set; }

    public string KindDisplay => Kind.DisplayName();

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public static SpecFileListItem FromPath(SpecFileKind kind, string path)
    {
        var status = string.IsNullOrWhiteSpace(path)
            ? "Not configured"
            : File.Exists(path) ? "Not loaded" : "File not found";
        return new SpecFileListItem { Kind = kind, Path = path, Status = status };
    }
}

public sealed record RawTokenRow(long Offset, string Text)
{
    public string OffsetHex => $"0x{Offset:X}";
}

public sealed record SpecRecordRow(int Index, long Offset, string Name, int Length)
{
    public string OffsetHex => $"0x{Offset:X}";
}
