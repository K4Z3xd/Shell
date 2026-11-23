using System.Diagnostics;
using System.IO;
using Shell.Configurator.Models;

namespace Shell.Configurator.Services;

public sealed record StartupResolution(string Directory, bool NeedsSetup, bool IsPortableDropIn);

public static class ShellService
{
    private const string PortablePointerFile = "shell.portable.path";

    public static StartupResolution ResolveStartupDirectory()
    {
        string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

        // 1. True drop-in portable: shell.nss already sits right next to ShellConfigurator.exe
        if (File.Exists(Path.Combine(baseDir, "shell.nss")))
            return new StartupResolution(baseDir, NeedsSetup: false, IsPortableDropIn: true);

        // 2. Saved portable pointer file next to ShellConfigurator.exe
        string pointerPath = Path.Combine(baseDir, PortablePointerFile);
        if (File.Exists(pointerPath))
        {
            string savedDir = File.ReadAllText(pointerPath).Trim();
            if (!string.IsNullOrWhiteSpace(savedDir) && File.Exists(Path.Combine(savedDir, "shell.nss")))
                return new StartupResolution(savedDir, NeedsSetup: false, IsPortableDropIn: true);
        }

        // 3. No config next to exe -> show clean first-run Setup (Import from Folder vs Fresh Config)
        string suggestedDefault = DetectSuggestedDirectory();
        return new StartupResolution(suggestedDefault, NeedsSetup: true, IsPortableDropIn: false);
    }

    public static string DetectSuggestedDirectory()
    {
        string programFilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Nilesoft Shell");
        if (File.Exists(Path.Combine(programFilesDir, "shell.nss")))
            return programFilesDir;

        return AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    }

    public static void SavePortablePointer(string targetDirectory)
    {
        try
        {
            string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            string pointerPath = Path.Combine(baseDir, PortablePointerFile);
            if (string.Equals(Path.GetFullPath(baseDir), Path.GetFullPath(targetDirectory), StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(pointerPath)) File.Delete(pointerPath);
                return;
            }
            File.WriteAllText(pointerPath, targetDirectory);
        }
        catch
        {
            // Ignore write errors in read-only locations
        }
    }

    public static (bool Success, string Message) CopyPortableAppIntoFolder(string targetDirectory)
    {
        try
        {
            string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            if (string.Equals(Path.GetFullPath(baseDir), Path.GetFullPath(targetDirectory), StringComparison.OrdinalIgnoreCase))
                return (true, "App is already inside the target folder.");

            Directory.CreateDirectory(targetDirectory);
            string[] candidateFiles = ["ShellConfigurator.exe", "shell.exe", "shell.dll"];
            int copied = 0;

            foreach (string file in candidateFiles)
            {
                string src = Path.Combine(baseDir, file);
                if (!File.Exists(src)) continue;
                string dst = Path.Combine(targetDirectory, file);
                if (!File.Exists(dst) || file.Equals("ShellConfigurator.exe", StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(src, dst, overwrite: true);
                    copied++;
                }
            }

            return (true, $"Copied {copied} portable file(s) into {targetDirectory}.");
        }
        catch (Exception ex)
        {
            return (false, $"Could not copy portable files: {ex.Message}");
        }
    }

    public static (bool Success, string Message) InitializeFreshConfig(string targetDirectory, bool copyBinaries, bool registerShell)
    {
        try
        {
            Directory.CreateDirectory(targetDirectory);
            string baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);

            // Copy supporting assets (images.nss, lang/*) if available near executable
            CopyIfPresent(Path.Combine(baseDir, "imports", "images.nss"), Path.Combine(targetDirectory, "imports", "images.nss"));
            string srcLang = Path.Combine(baseDir, "imports", "lang");
            if (Directory.Exists(srcLang))
            {
                string dstLang = Path.Combine(targetDirectory, "imports", "lang");
                Directory.CreateDirectory(dstLang);
                foreach (string file in Directory.GetFiles(srcLang))
                    CopyIfPresent(file, Path.Combine(dstLang, Path.GetFileName(file)));
            }

            if (copyBinaries)
                CopyPortableAppIntoFolder(targetDirectory);

            var fresh = ShellConfig.CreateDefault();
            NssSerializer.SaveToDirectory(targetDirectory, fresh, createBackup: false);
            SavePortablePointer(targetDirectory);

            if (registerShell && HasShellBinary(targetDirectory))
            {
                var (ok, msg) = RunShellExe(targetDirectory, "-register", requireAdmin: true);
                return ok
                    ? (true, $"Fresh config created and Shell registered in {targetDirectory}.")
                    : (true, $"Fresh config created in {targetDirectory} (registration note: {msg}).");
            }

            return (true, $"Fresh portable config created in {targetDirectory}.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to initialize fresh config: {ex.Message}");
        }
    }

    private static void CopyIfPresent(string src, string dst)
    {
        if (!File.Exists(src)) return;
        if (string.Equals(Path.GetFullPath(src), Path.GetFullPath(dst), StringComparison.OrdinalIgnoreCase)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        File.Copy(src, dst, overwrite: true);
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
