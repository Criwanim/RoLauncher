using System.Diagnostics;
using System.IO;

namespace RoLauncher.Services;

public sealed class GameLaunchService
{
    public Process? Start(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            throw new FileNotFoundException("Executável da conta não encontrado.", executablePath);
        }

        var info = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty,
            UseShellExecute = true
        };

        return Process.Start(info);
    }
}
