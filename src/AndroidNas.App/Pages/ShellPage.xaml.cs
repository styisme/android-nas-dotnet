using AndroidNas.Core;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace AndroidNas.App.Pages;

public partial class ShellPage : ContentPage
{
    public ShellPage()
    {
        InitializeComponent();
        Output.Text = "Welcome to Android NAS Shell\n"
                    + "以 # 开头的行会被忽略，危险命令会被拦截\n\n";
    }

    private async void OnCheckRoot(object sender, EventArgs e)
    {
        RootLabel.Text = "检测中…";
        var ok = await Task.Run(() => RootShell.HasRoot());
        RootLabel.Text = ok ? "✅ 已获取" : "❌ 未获取";
    }

    private async void OnRunCommand(object sender, EventArgs e)
    {
        var cmd = CommandInput.Text?.Trim();
        if (string.IsNullOrEmpty(cmd)) return;
        CommandInput.Text = "";

        Append("$ " + cmd + "\n");

        if (IsDangerous(cmd))
        {
            Append("⛔ 已拦截：该命令被判定为高危操作\n\n");
            return;
        }

        try
        {
            var output = await Task.Run(() => RootShell.Exec(cmd));
            Append(string.IsNullOrEmpty(output) ? "(无输出)\n\n" : output + "\n\n");
        }
        catch (Exception ex)
        {
            Append("Error: " + ex.Message + "\n\n");
        }
    }

    private static bool IsDangerous(string cmd)
    {
        if (cmd.Contains("rm -rf /")) return true;
        if (cmd.Contains("mkfs")) return true;
        if (cmd.Contains("dd if=")) return true;
        if (cmd.Contains(":(){:|:&};:")) return true;
        return false;
    }

    private void Append(string text)
    {
        Output.Text = (Output.Text ?? "") + text;
        try
        {
            var editor = Output;
            if (editor != null) editor.CursorPosition = editor.Text?.Length ?? 0;
        }
        catch { }
    }

    private void OnClear(object sender, EventArgs e)
    {
        Output.Text = "已清空\n\n";
    }

    private async void OnCopy(object sender, EventArgs e)
    {
        await Clipboard.Default.SetTextAsync(Output.Text ?? "");
        await DisplayAlert("完成", "输出已复制到剪贴板", "好");
    }
}
