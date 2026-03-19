namespace RoLauncher.Models;

public sealed class AppSettings
{
    public string GameInstallPath { get; set; } = string.Empty;
    public string RuntimeTokenPath { get; set; } = string.Empty;
    public string InstancesRootPath { get; set; } = string.Empty;

    public List<TokenRule> TokenRules { get; set; } = new();
    public List<AccountProfile> Accounts { get; set; } = new();
}