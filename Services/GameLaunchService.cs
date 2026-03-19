using System.Diagnostics;
using System.IO;

namespace RoLauncher.Services;

public sealed class GameLaunchService
{
    public Process? Start(string executablePath)
    {
        var info = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath)!,
            UseShellExecute = true
        };

        return Process.Start(info);
    }
}