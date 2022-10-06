using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;

namespace KangDroid.Azsync;

public class FetchAppService
{
    private readonly string _appServiceName;
    private readonly ArmClient _armClient;
    private readonly string _resourceGroupName;
    private readonly string? _slotName;

    public FetchAppService(string resourceGroupName, string appServiceName, string? slotName)
    {
        _armClient = new ArmClient(new DefaultAzureCredential());
        _resourceGroupName = resourceGroupName;
        _appServiceName = appServiceName;
        _slotName = slotName;
    }

    public async Task<AzSyncResponse<Dictionary<string, string>>> ExecuteAsync()
    {
        var subscription = await _armClient.GetDefaultSubscriptionAsync();

        // Resource Group
        var resourceGroup = await subscription.GetResourceGroupAsync(_resourceGroupName);
        if (!resourceGroup.Value.HasData)
        {
            return new AzSyncResponse<Dictionary<string, string>>(
                $"FetchAppService.ExecuteAsync: Cannot get resource group, name: {_resourceGroupName}");
        }

        // App Service
        var appService = await resourceGroup.Value.GetWebSiteAsync(_appServiceName);
        if (!resourceGroup.Value.HasData)
        {
            return new AzSyncResponse<Dictionary<string, string>>(
                $"FetchAppService.ExecuteAsync: Cannot get app service, name: {_appServiceName}");
        }

        // App Service Plan(Validation for OS Type)
        var appServicePlan =
            await _armClient.GetAppServicePlanResource(new ResourceIdentifier(appService.Value.Data.AppServicePlanId))
                            .GetAsync();
        if (!appServicePlan.Value.HasData)
        {
            return new AzSyncResponse<Dictionary<string, string>>(
                $"FetchAppService.ExecuteAsync: Cannot get App Service Plan, Resource ID: {appService.Value.Data.AppServicePlanId}");
        }

        // Check Linux
        var isLinux = appServicePlan.Value.Data.Kind == "linux";

        // Get configuration
        var configuration = _slotName != null
            ? await GetSlotConfigurations(appService.Value, isLinux)
            : await GetProductionConfigurations(appService.Value, isLinux);
        if (configuration == null)
        {
            return new AzSyncResponse<Dictionary<string, string>>(
                $"FetchAppService.ExecuteAsync: Cannot get configuration - Name: {_appServiceName}, Slot: {_slotName}");
        }

        return new AzSyncResponse<Dictionary<string, string>>
        {
            IsError = false,
            Result = configuration
        };
    }

    private async Task<Dictionary<string, string>?> GetProductionConfigurations(
        WebSiteResource productionResource, bool isLinux)
    {
        var settings = await productionResource.GetApplicationSettingsAsync();
        var appSettingsRaw = settings.Value.Properties;
        if (appSettingsRaw == null) return null;

        IDictionary<string, string> appSettings =
            isLinux ? appSettingsRaw.ToDictionary(a => a.Key.Replace("__", ":"), a => a.Value) : appSettingsRaw;

        var connection = await productionResource.GetConnectionStringsAsync();
        var connectionStringsRaw = connection.Value.Properties;
        if (connectionStringsRaw == null) return null;

        var connectionString = connectionStringsRaw.ToDictionary(a => $"ConnectionStrings:{a.Key}", a => a.Value.Value);

        return appSettings.Concat(connectionString).ToDictionary(a => a.Key, a => a.Value);
    }

    private async Task<Dictionary<string, string>?> GetSlotConfigurations(WebSiteResource productionResource, bool isLinux)
    {
        var slot = await productionResource.GetWebSiteSlotAsync(_slotName);
        if (!slot.Value.HasData) return null;

        var settings = await slot.Value.GetApplicationSettingsSlotAsync();
        var appSettingsRaw = settings.Value.Properties;
        if (appSettingsRaw == null) return null;

        IDictionary<string, string> appSettings =
            isLinux ? appSettingsRaw.ToDictionary(a => a.Key.Replace("__", ":"), a => a.Value) : appSettingsRaw;

        var connection = await slot.Value.GetConnectionStringsSlotAsync();
        var connectionStringsRaw = connection.Value.Properties;
        if (connectionStringsRaw == null) return null;

        var connectionString = connectionStringsRaw.ToDictionary(a => $"ConnectionStrings:{a.Key}", a => a.Value.Value);

        return appSettings.Concat(connectionString).ToDictionary(a => a.Key, a => a.Value);
    }
}