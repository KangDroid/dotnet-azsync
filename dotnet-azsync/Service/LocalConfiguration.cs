using KangDroid.Azsync.Models;
using Newtonsoft.Json;

namespace KangDroid.Azsync.Service;

public class LocalConfiguration : IConfigurationFetcher
{
    private const string AppSettingsPath = ".deployment/test/appservice_appsettings.json";
    private const string ConnectionStringsPath = ".deployment/test/appservice_connections.json";

    public async Task<AzSyncResponse<Dictionary<string, string>>> GetConfigurations()
    {
        // File Validation
        if (!File.Exists(AppSettingsPath))
        {
            return new AzSyncResponse<Dictionary<string, string>>
            {
                IsError = true,
                Message = $"Cannot open file {AppSettingsPath} - No Such file or directory!"
            };
        }

        if (!File.Exists(ConnectionStringsPath))
        {
            return new AzSyncResponse<Dictionary<string, string>>
            {
                IsError = true,
                Message = $"Cannot open file {ConnectionStringsPath} - No Such file or directory!"
            };
        }

        // Read Files
        var appSettingsStr = await ReadFileAsync(AppSettingsPath);
        var connectionStringsStr = await ReadFileAsync(ConnectionStringsPath);

        // Parse
        var appSettingsDictionary = ParseAppSettings(appSettingsStr);
        var targetDictionary = appSettingsDictionary.Concat(ParseConnectionSettings(connectionStringsStr))
                                                    .ToDictionary(a => a.Key, a => a.Value);

        return new AzSyncResponse<Dictionary<string, string>>
        {
            Result = targetDictionary
        };
    }

    private async Task<string> ReadFileAsync(string path)
    {
        // Open Read File
        await using var file = File.OpenRead(path);

        // Create Readable Stream
        using var streamReader = new StreamReader(path);

        // Return Direct Stream Reader
        return await streamReader.ReadToEndAsync();
    }

    private Dictionary<string, string> ParseAppSettings(string appSettingsStr)
    {
        // Get App Settings List
        var appSettingList = JsonConvert.DeserializeObject<List<LocalAppSettings>>(appSettingsStr)
                             ?? throw new Exception($"Cannot deserialize application settings: {appSettingsStr}");

        // App Settings List to Dictionary (Make sure '__' for linux configuration converted to ':')
        return appSettingList.ToDictionary(a => a.Name.Replace("__", ":"), a => a.Value);
    }

    private Dictionary<string, string> ParseConnectionSettings(string connectionStringsStr)
    {
        // Get Connection String List
        var connectionStringsList = JsonConvert.DeserializeObject<List<LocalConnectionStrings>>(connectionStringsStr)
                                    ?? throw new Exception($"Cannot deserialize connection settings: {connectionStringsStr}");

        // Connection String to Dictionary
        return connectionStringsList.ToDictionary(a => $"ConnectionStrings:{a.Name}", a => a.Value);
    }
}