using AndroidNas.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;

namespace AndroidNas.App.Pages;

public partial class PreviewPage : ContentPage
{
    private readonly string _physicalPath;
    private readonly string _name;

    public PreviewPage(string physicalPath, string name)
    {
        InitializeComponent();
        _physicalPath = physicalPath;
        _name = name;
        Title = name;
        NameLabel.Text = name;

        long size = 0;
        try { size = new FileInfo(physicalPath).Length; } catch { }
        InfoLabel.Text = "大小: " + FormatSize(size);

        LoadPreview();
    }

    private void LoadPreview()
    {
        var ext = Path.GetExtension(_name).ToLowerInvariant();
        if (IsImage(ext))
        {
            try
            {
                var source = ImageSource.FromFile(_physicalPath);
                ImagePreview.Source = source;
                ImagePreview.IsVisible = true;
                FallbackLabel.IsVisible = false;
            }
            catch (Exception e)
            {
                FallbackLabel.Text = "图片加载失败: " + e.Message;
            }
        }
        else if (IsVideo(ext) || IsAudio(ext))
        {
            FallbackLabel.Text = "视频/音频请点\"外部打开\"用系统播放器播放";
        }
        else if (IsText(ext))
        {
            try
            {
                var content = File.ReadAllText(_physicalPath);
                if (content.Length > 20000) content = content.Substring(0, 20000) + "\n\n…(预览截断)";
                FallbackLabel.Text = content;
            }
            catch (Exception e)
            {
                FallbackLabel.Text = "文本读取失败: " + e.Message;
            }
        }
        else
        {
            FallbackLabel.Text = "此文件类型不支持内嵌预览，请点\"外部打开\"";
        }
    }

    private async void OnExternal(object sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(_physicalPath),
                Title = _name
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("错误", "无法打开: " + ex.Message, "好");
        }
    }

    private async void OnClose(object sender, EventArgs e) => await Navigation.PopAsync();

    private static bool IsImage(string ext)
    {
        switch (ext)
        {
            case ".jpg": case ".jpeg": case ".png": case ".gif":
            case ".webp": case ".bmp": case ".svg":
                return true;
            default: return false;
        }
    }

    private static bool IsVideo(string ext)
    {
        switch (ext)
        {
            case ".mp4": case ".mkv": case ".webm": case ".avi": case ".mov": case ".flv":
                return true;
            default: return false;
        }
    }

    private static bool IsAudio(string ext)
    {
        switch (ext)
        {
            case ".mp3": case ".wav": case ".flac": case ".ogg": case ".m4a": case ".aac":
                return true;
            default: return false;
        }
    }

    private static bool IsText(string ext)
    {
        switch (ext)
        {
            case ".txt": case ".md": case ".json": case ".xml": case ".cs":
            case ".java": case ".js": case ".html": case ".css": case ".py":
            case ".log": case ".ini": case ".yml": case ".yaml": case ".sh":
            case ".conf": case ".csv": case ".sql":
                return true;
            default: return false;
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1048576) return (bytes / 1024.0).ToString("F1") + " KB";
        if (bytes < 1073741824L) return (bytes / 1048576.0).ToString("F1") + " MB";
        return (bytes / 1073741824.0).ToString("F2") + " GB";
    }
}
