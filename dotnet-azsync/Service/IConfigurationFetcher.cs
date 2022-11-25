namespace KangDroid.Azsync.Service;

public interface IConfigurationFetcher
{
    public Task<AzSyncResponse<Dictionary<string, string>>> GetConfigurations();
}