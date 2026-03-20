namespace RoLauncher.Models;

public sealed class AppSettings
{
    // 🎮 Pasta de instalação do jogo
    public string GameInstallPath { get; set; } = string.Empty;

    // 📁 AppData LocalLow — pasta base
    public string AppDataBasePath { get; set; } = string.Empty;

    // 📁 AppData LocalLow — pasta XD\PC (dados importantes)
    public string AppDataPcPath { get; set; } = string.Empty;

    // 📂 Pasta raiz das instâncias clonadas
    public string InstancesRootPath { get; set; } = string.Empty;

    public List<TokenRule> TokenRules { get; set; } = new();
    public List<AccountProfile> Accounts { get; set; } = new();
}