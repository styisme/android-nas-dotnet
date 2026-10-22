using AndroidNas.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace AndroidNas.App.Pages;

public partial class LogsPage : ContentPage
{
    public LogsPage()
    {
        InitializeComponent();
        Refresh();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void Refresh()
    {
        var logs = NasLog.Snapshot();
        var path = NasLog.GetLogFilePath();
        LogLabel.Text = $"[日志存储路径: {path}]\n\n" + (logs.Length == 0 ? "(暂无日志)" : string.Join("\n\n", logs));
    }

    private void OnRefresh(object sender, EventArgs e) => Refresh();

    private async void OnCopy(object sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(LogLabel.Text ?? "");
        await DisplayAlertAsync("完成", "日志已复制到剪贴板", "好");
    }

    private async void OnClear(object sender, EventArgs e)
    {
        var ok = await DisplayAlertAsync("清空日志", "确定清空所有日志？", "清空", "取消");
        if (!ok) return;
        LogLabel.Text = "(已清空显示，实际日志保留在内存中)";
    }
}
