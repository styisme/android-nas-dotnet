using System.IO.Compression;
using AndroidNas.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace AndroidNas.App.Pages;

public partial class FilesPage : ContentPage
{
    private string _currentPath = "/";

    public FilesPage()
    {
        InitializeComponent();
        LoadList();
    }

    private void OnRefresh(object sender, EventArgs e)
    {
        LoadList();
        if (Refresher != null) Refresher.IsRefreshing = false;
    }

    private void LoadList()
    {
        if (PathLabel == null) return;
        PathLabel.Text = _currentPath;

        var entries = new System.Collections.Generic.List<FileEntry>();
        try
        {
            var root = AppSettings.RootPath;
            var physical = SafePath.Resolve(root, _currentPath);
            if (!Directory.Exists(physical))
            {
                _currentPath = "/";
                physical = SafePath.Resolve(root, "/");
            }

            foreach (var entry in Directory.EnumerateFileSystemEntries(physical))
            {
                var isDir = Directory.Exists(entry);
                var name = Path.GetFileName(entry);
                long size = 0;
                if (!isDir)
                {
                    try { size = new FileInfo(entry).Length; } catch { }
                }

                entries.Add(new FileEntry
                {
                    Name = name,
                    FullPath = SafePath.ToVirtual(root, entry),
                    Physical = entry,
                    IsDir = isDir,
                    Icon = isDir ? "📁" : PickIcon(name),
                    Detail = isDir ? "文件夹" : FormatSize(size)
                });
            }

            entries.Sort((a, b) =>
            {
                if (a.IsDir != b.IsDir) return a.IsDir ? -1 : 1;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }
        catch (Exception ex)
        {
            HintLabel.Text = "读取失败: " + ex.Message;
        }

        FileList.ItemsSource = entries;
        HintLabel.Text = entries.Count == 0 ? "空文件夹" : (entries.Count + " 项");
    }

    private async void OnUp(object sender, EventArgs e)
    {
        if (_currentPath == "/" || string.IsNullOrEmpty(_currentPath)) return;
        var trimmed = _currentPath.TrimEnd('/');
        var idx = trimmed.LastIndexOf('/');
        _currentPath = idx <= 0 ? "/" : trimmed.Substring(0, idx);
        LoadList();
        await Task.CompletedTask;
    }

    private async void OnFileTapped(object sender, TappedEventArgs e)
    {
        var grid = sender as Grid;
        var entry = grid?.BindingContext as FileEntry;
        if (entry == null) return;

        if (entry.IsDir)
        {
            _currentPath = entry.FullPath;
            LoadList();
            return;
        }

        await ShowFileMenu(entry);
    }

    private async Task ShowFileMenu(FileEntry entry)
    {
        var ext = Path.GetExtension(entry.Name).ToLowerInvariant();
        var isText = IsTextExt(ext);
        var isZip = ext == ".zip";

        var actions = new System.Collections.Generic.List<string> { "预览" };
        actions.Add("分享/打开");
        if (isText) actions.Add("编辑");
        if (isZip) actions.Add("解压");
        actions.Add("重命名");
        actions.Add("删除");

        var choice = await DisplayActionSheetAsync(entry.Name, "取消", null,
            actions.ToArray());

        if (string.IsNullOrEmpty(choice) || choice == "取消") return;

        switch (choice)
        {
            case "预览":
                await Navigation.PushAsync(new PreviewPage(entry.Physical, entry.Name));
                break;

            case "分享/打开":
                await Share.RequestAsync(new ShareFileRequest
                {
                    Title = entry.Name,
                    File = new ShareFile(entry.Physical)
                });
                break;

            case "编辑":
                await EditTextAsync(entry);
                break;

            case "解压":
                await UnzipAsync(entry);
                break;

            case "重命名":
                await RenameAsync(entry);
                break;

            case "删除":
                await DeleteAsync(entry);
                break;
        }
    }

    private async Task EditTextAsync(FileEntry entry)
    {
        string original;
        try { original = File.ReadAllText(entry.Physical); }
        catch (Exception ex) { await DisplayAlertAsync("错误", "读取失败: " + ex.Message, "好"); return; }

        var editor = new Editor
        {
            Text = original,
            HeightRequest = 400,
            FontFamily = "Monospace",
            FontSize = 13,
            BackgroundColor = Colors.White
        };

        var saveBtn = new Button
        {
            Text = "保存",
            Style = Application.Current?.Resources.TryGetValue("MdButtonFilled", out var s1) == true ? s1 as Style : null
        };
        var cancelBtn = new Button
        {
            Text = "取消",
            Style = Application.Current?.Resources.TryGetValue("MdButtonTonal", out var s2) == true ? s2 as Style : null
        };

        var page = new ContentPage
        {
            Title = entry.Name,
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = 16,
                    Spacing = 12,
                    Children = { editor, saveBtn, cancelBtn }
                }
            }
        };

        saveBtn.Clicked += async (s, ev) =>
        {
            try
            {
                File.WriteAllText(entry.Physical, editor.Text ?? "");
                await Navigation.PopModalAsync();
                LoadList();
                await DisplayAlertAsync("完成", "已保存", "好");
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("错误", "保存失败: " + ex.Message, "好");
            }
        };
        cancelBtn.Clicked += async (s, ev) => await Navigation.PopModalAsync();

        await Navigation.PushModalAsync(new NavigationPage(page));
    }

    private async Task UnzipAsync(FileEntry entry)
    {
        try
        {
            var targetDir = Path.Combine(
                Path.GetDirectoryName(entry.Physical)!,
                Path.GetFileNameWithoutExtension(entry.Name));
            Directory.CreateDirectory(targetDir);
            var targetFull = Path.GetFullPath(targetDir);

            await Task.Run(() =>
            {
                using var archive = ZipFile.OpenRead(entry.Physical);
                foreach (var item in archive.Entries)
                {
                    var dest = Path.GetFullPath(Path.Combine(targetDir, item.FullName));
                    if (!dest.StartsWith(targetFull, StringComparison.Ordinal)) continue;
                    if (string.IsNullOrEmpty(item.Name))
                    {
                        Directory.CreateDirectory(dest);
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        item.ExtractToFile(dest, true);
                    }
                }
            });

            LoadList();
            await DisplayAlertAsync("完成", "已解压到 " + Path.GetFileName(targetDir), "好");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", "解压失败: " + ex.Message, "好");
        }
    }

    private async Task RenameAsync(FileEntry entry)
    {
        var newName = await DisplayPromptAsync("重命名", "新名称", initialValue: entry.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName == entry.Name) return;
        if (newName.IndexOf('/') >= 0 || newName.IndexOf('\\') >= 0 || newName == "..")
        {
            await DisplayAlertAsync("错误", "名称含非法字符", "好");
            return;
        }

        try
        {
            var parent = Path.GetDirectoryName(entry.Physical)!;
            var newPath = Path.Combine(parent, newName);
            if (entry.IsDir) Directory.Move(entry.Physical, newPath);
            else File.Move(entry.Physical, newPath);
            LoadList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", "重命名失败: " + ex.Message, "好");
        }
    }

    private async Task DeleteAsync(FileEntry entry)
    {
        var ok = await DisplayAlertAsync("删除", "确定删除 " + entry.Name + "？", "删除", "取消");
        if (!ok) return;
        try
        {
            if (entry.IsDir) Directory.Delete(entry.Physical, true);
            else File.Delete(entry.Physical);
            LoadList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", "删除失败: " + ex.Message, "好");
        }
    }

    private async void OnMkdir(object sender, EventArgs e)
    {
        var name = await DisplayPromptAsync("新建文件夹", "名称");
        if (string.IsNullOrWhiteSpace(name)) return;
        if (name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0 || name == "..")
        {
            await DisplayAlertAsync("错误", "名称含非法字符", "好");
            return;
        }

        try
        {
            var root = AppSettings.RootPath;
            var physical = SafePath.Resolve(root, _currentPath);
            Directory.CreateDirectory(Path.Combine(physical, name));
            LoadList();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", "创建失败: " + ex.Message, "好");
        }
    }

    private async void OnUpload(object sender, EventArgs e)
    {
        try
        {
            var files = await FilePicker.Default.PickMultipleAsync();
            if (files == null) return;

            var root = AppSettings.RootPath;
            var physical = SafePath.Resolve(root, _currentPath);
            var count = 0;

            foreach (var file in files)
            {
                var target = Path.Combine(physical, Path.GetFileName(file.FileName ?? "upload.tmp"));
                await using var src = await file.OpenReadAsync();
                await using var dst = File.Create(target);
                await src.CopyToAsync(dst);
                count++;
            }

            LoadList();
            await DisplayAlertAsync("完成", "已上传 " + count + " 个文件", "好");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", "上传失败: " + ex.Message, "好");
        }
    }

    private static bool IsTextExt(string ext)
    {
        switch (ext)
        {
            case ".txt": case ".md": case ".json": case ".cs":
            case ".java": case ".js": case ".html": case ".xml":
            case ".css": case ".py": case ".log": case ".ini":
            case ".yml": case ".yaml": case ".conf": case ".sh":
                return true;
            default: return false;
        }
    }

    private static string PickIcon(string name)
    {
        var ext = Path.GetExtension(name).ToLowerInvariant();
        switch (ext)
        {
            case ".jpg": case ".jpeg": case ".png": case ".gif": case ".webp":
                return "🖼️";
            case ".mp4": case ".mkv": case ".webm": case ".avi":
                return "🎬";
            case ".mp3": case ".wav": case ".flac": case ".ogg":
                return "🎵";
            case ".zip": case ".tar": case ".gz": case ".rar": case ".7z":
                return "📦";
            case ".txt": case ".md": case ".log":
                return "📝";
            case ".pdf":
                return "📕";
            case ".apk":
                return "📱";
            default:
                return "📄";
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
        if (bytes < 1024L * 1024 * 1024) return (bytes / 1024.0 / 1024).ToString("F1") + " MB";
        return (bytes / 1024.0 / 1024 / 1024).ToString("F2") + " GB";
    }

    public class FileEntry
    {
        public string Icon { get; set; } = "";
        public string Name { get; set; } = "";
        public string Detail { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string Physical { get; set; } = "";
        public bool IsDir { get; set; }
    }
}
