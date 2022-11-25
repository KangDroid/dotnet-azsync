// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using KangDroid.Azsync.Models;
using KangDroid.Azsync.Service;
using Newtonsoft.Json;

// root command
var rootCommand = new RootCommand("Azure App Service Configuration -> Local Secret.json sync");

// App Service Subcommand Area
var appServiceNameOption = new Option<string>("--name", "[Required] App Service's name.");
var resourceGroupOption = new Option<string>("--resource-group", "[Required] Resource Group");
var appServiceSlotOption = new Option<string>("--slot", "[Optional] App Service's Slot");
var appServiceCommand = new Command("appservice", "Fetch App Service configuration to local secrets.")
{
    appServiceNameOption,
    resourceGroupOption,
    appServiceSlotOption
};
appServiceCommand.SetHandler(async (name, resourceGroup, slot) =>
{
    var fetchService = new FetchAppService(resourceGroup, name, slot);
    var result = await fetchService.GetConfigurations();
    if (result.IsError) Console.WriteLine(result.Message);

    var projectFileService = new UserSecretService();
    var response = await projectFileService.ExecuteAsync(JsonConvert.SerializeObject(result.Result));

    if (response.IsError) Console.WriteLine(response.Message);
}, appServiceNameOption, resourceGroupOption, appServiceSlotOption);


// Local Configuration Subcommand Area
var projectOption = new Option<string>("--project", "[Required] Project(*.csproj) Path")
{
    IsRequired = true
};
var localSubcommand = new Command("local",
    "Load local configuration(PROJECT_ROOT/.deployment/test/appservice_*.json) to user secret store")
{
    projectOption
};
localSubcommand.SetHandler(async projectPath =>
{
    var localService = new LocalConfiguration();
    var localConfiguration = await localService.GetConfigurations();
    if (localConfiguration.IsError) Console.WriteLine(localConfiguration.Message);

    var projectFileService = new UserSecretService();
    var response = await projectFileService.ExecuteAsync(JsonConvert.SerializeObject(localConfiguration.Result), projectPath);

    if (response.IsError) Console.WriteLine(response.Message);
}, projectOption);

// Connection Strings Command Generator
var connectionStringPath = new Option<string>("--connection-json-path", "[Required] Connection String(Json) Path")
{
    IsRequired = true
};
var commandGenerator = new Command("connection-generate",
    "[Development] Produces AZ CLI Shell Script for updating connection strings.")
{
    appServiceNameOption,
    resourceGroupOption,
    appServiceSlotOption,
    connectionStringPath
};
commandGenerator.SetHandler(async (name, resourceGroup, slot, connectionStringFilePath) =>
{
    // az webapp config connection-string set -g ${resourceGroup} -n ${serviceName} -t ${eachJsonSettings[i].type} --settings ${eachJsonSettings[i].name}='${eachJsonSettings[i].value}'

    if (!File.Exists(connectionStringFilePath))
    {
        Console.WriteLine($"Error: Cannot get connection string path: {connectionStringPath}");
        return;
    }

    await using var file = File.OpenRead(connectionStringFilePath);
    using var stream = new StreamReader(file);
    var str = await stream.ReadToEndAsync();
    var connectionList = JsonConvert.DeserializeObject<List<LocalConnectionStrings>>(str);

    foreach (var eachConnection in connectionList)
    {
        var slotEnabled = slot == null ? "" : $"-s {slot}";
        var basicCommand = "";

        if (eachConnection.SlotSetting)
        {
            basicCommand =
                $"az webapp config connection-string set -g {resourceGroup} -n {name} {slotEnabled} -t {eachConnection.Type} --slot-settings \"{eachConnection.Name}='{eachConnection.Value}'\"";
        }
        else
        {
            basicCommand =
                $"az webapp config connection-string set -g {resourceGroup} -n {name} {slotEnabled} -t {eachConnection.Type} --settings \"{eachConnection.Name}='{eachConnection.Value}'\"";
        }

        Console.WriteLine(basicCommand);
    }
}, appServiceNameOption, resourceGroupOption, appServiceSlotOption, connectionStringPath);

// Add Subcommands to RootCommand
rootCommand.AddCommand(appServiceCommand);
rootCommand.AddCommand(localSubcommand);
rootCommand.AddCommand(commandGenerator);

await rootCommand.InvokeAsync(args);