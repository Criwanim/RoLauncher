using RoLauncher.Models;
using System.IO;

namespace RoLauncher.Services;

public sealed class ShortcutService
{
    public string CreateDesktopShortcut(AccountProfile account)
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string shortcutPath = Path.Combine(desktop, $"{account.Code}.lnk");

        Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
            throw new InvalidOperationException("WScript.Shell não está disponível.");

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);

        shortcut.TargetPath = account.ExecutablePath;
        shortcut.WorkingDirectory = account.InstanceFolderPath;
        shortcut.IconLocation = account.ExecutablePath;
        shortcut.Save();

        return shortcutPath;
    }
}