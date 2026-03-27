namespace RoLauncher.Services;

public sealed class EnvironmentProgress
{
    public int Percentage { get; init; }
    public string Message { get; init; } = string.Empty;
}
