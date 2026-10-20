using AndroidNas.Core;

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
        LogLabel.Text = logs.Length == 0 ? "(暂无日志)" : string.Join("\n\n", logs);
    }

    private void OnRefresh(object sender, EventArgs e) => Refresh();

    private async void OnCopy(object sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(LogLabel.Text ?? "");
        await DisplayAlert("完成", "日志已复制到剪贴板", "好");
    }

    private async void OnClear(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("清空日志", "确定清空所有日志？", "清空", "取消");
        if (!ok) return;

        // NasLog 没有 clear，用反射清空或留空提示
        LogLabel.Text = "(已清空显示，实际日志保留在内存中)";
    }
}
