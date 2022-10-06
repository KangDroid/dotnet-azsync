// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using KangDroid.Azsync;
using Newtonsoft.Json;

// root command
var rootCommand = new RootCommand("Azure App Service Configuration -> Local Secret.json sync");

// App Service Subcommand Area
var appServiceNameOption = new Option<string>("--name", "[Required] App Service's name.");
var resourceGroupOption = new Option<string>("--resource-group", "[Required] Resource Group");
var appServiceSlotOption = new Option<string>("--slot", "[Optional] App Service's Slot");
var command = new Command("appservice", "Fetch App Service configuration to local secrets.")
{
    appServiceNameOption,
    resourceGroupOption,
    appServiceSlotOption
};

command.SetHandler(async (name, resourceGroup, slot) =>
{
    var fetchService = new FetchAppService(resourceGroup, name, slot);
    var result = await fetchService.ExecuteAsync();
    if (result.IsError) Console.WriteLine(result.Message);

    var projectFileService = new ProjectFileService();
    var response = await projectFileService.ExecuteAsync(JsonConvert.SerializeObject(result.Result));

    if (response.IsError) Console.WriteLine(response.Message);
}, appServiceNameOption, resourceGroupOption, appServiceSlotOption);
rootCommand.AddCommand(command);

await rootCommand.InvokeAsync(args);