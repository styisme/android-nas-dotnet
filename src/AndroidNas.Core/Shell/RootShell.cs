using System.Diagnostics;

namespace AndroidNas.Core;

public static class RootShell
{
    public static string Exec(string cmd)
    {
        try
        {
            var psi = new ProcessStartInfo("su")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return "Error: cannot start su";
            p.StandardInput.WriteLine(cmd);
            p.StandardInput.WriteLine("exit");
            p.StandardInput.Flush();
            var output = p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit(15000);
            return string.IsNullOrEmpty(output) ? err : output;
        }
        catch (Exception e) { return "Error: " + e.Message; }
    }

    public static bool HasRoot()
    {
        try { return Exec("id").Contains("uid=0"); }
        catch { return false; }
    }
}
