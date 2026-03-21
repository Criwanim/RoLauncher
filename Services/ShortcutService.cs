using System.IO;
using RoLauncher.Models;

namespace RoLauncher.Services;

public sealed class ShortcutService
{
    public string CreateDesktopShortcut(AccountProfile account)
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var shortcutPath = Path.Combine(desktop, $"{account.Code}.lnk");
        var shellType = Type.GetTypeFromProgID("WScript.Shell");

        if (shellType is null)
        {
            throw new InvalidOperationException("WScript.Shell não está disponível neste ambiente.");
        }

        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = account.ExecutablePath;
        shortcut.WorkingDirectory = account.InstanceFolderPath;
        shortcut.IconLocation = account.ExecutablePath;
        shortcut.Save();
        return shortcutPath;
    }
}
