namespace RoLauncher.Models;

public sealed class AccountProfile
{
    public int SlotNumber { get; set; }
    public string Code { get; set; } = string.Empty;         // ro_win1, ro_win2...
    public string DisplayName { get; set; } = string.Empty;
    public string InstanceFolderPath { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string ShortcutPath { get; set; } = string.Empty;
    public string BackupTokenFolderPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLaunchAt { get; set; }
}