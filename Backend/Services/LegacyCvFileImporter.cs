namespace JobPortal.Services;

public sealed class LegacyCvFileImporter(IServiceScopeFactory scopes, IConfiguration configuration) : IHostedService
{
    public Task StartAsync(CancellationToken token)
    {
        if (!configuration.GetValue("Storage:ImportLegacyFiles", true)) return Task.CompletedTask;
        using var scope = scopes.CreateScope();
        scope.ServiceProvider.GetRequiredService<PrivateFileStorage>().ImportLegacyFiles();
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken token) => Task.CompletedTask;
}
