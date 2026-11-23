using System.Diagnostics;
using System.IO;

namespace Shell.Configurator.Services;

public static class ShellService
{
    public static string DetectShellDirectory()
    {
        string baseDir = AppContext.BaseDirectory;
        if (File.Exists(Path.Combine(baseDir, "shell.nss")))
            return baseDir.TrimEnd(Path.DirectorySeparatorChar);

        string programFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Nilesoft Shell");
        if (File.Exists(Path.Combine(programFilesDir, "shell.nss")))
            return programFilesDir;

        // Walk upward to locate repo src\bin when running from build output
        var dir = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && dir != null; i++)
        {
            string candidate = Path.Combine(dir.FullName, "src", "bin");
            if (File.Exists(Path.Combine(candidate, "shell.nss")))
                return candidate;
            string directBin = Path.Combine(dir.FullName, "bin");
            if (File.Exists(Path.Combine(directBin, "shell.nss")))
                return directBin;
            dir = dir.Parent;
        }

        return baseDir.TrimEnd(Path.DirectorySeparatorChar);
    }

    public static bool HasShellBinary(string directory)
        => File.Exists(Path.Combine(directory, "shell.exe"));

    public static (bool Success, string Message) RunShellExe(string directory, string arguments, bool requireAdmin = true)
    {
        string exePath = Path.Combine(directory, "shell.exe");
        if (!File.Exists(exePath))
            return (false, $"shell.exe was not found in '{directory}'.");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                WorkingDirectory = directory,
                UseShellExecute = true
            };
            if (requireAdmin)
                psi.Verb = "runas";

            using var proc = Process.Start(psi);
            proc?.WaitForExit(10000);
            return (true, $"Executed shell.exe {arguments}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static (bool Success, string Message) RestartExplorer()
    {
        try
        {
            foreach (var proc in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    proc.Kill();
                    proc.WaitForExit(3000);
                }
                catch
                {
                    // Ignore individual termination failures
                }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                UseShellExecute = true
            });
            return (true, "Windows Explorer restarted.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to restart Explorer: {ex.Message}");
        }
    }

    public static void OpenFolder(string directory)
    {
        if (!Directory.Exists(directory))
            return;
        Process.Start(new ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true
        });
    }
}
