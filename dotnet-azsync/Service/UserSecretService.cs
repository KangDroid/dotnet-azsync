using Microsoft.Build.Construction;

namespace KangDroid.Azsync.Service;

public class UserSecretService
{
    public async Task<AzSyncResponse<object>> ExecuteAsync(string obj)
    {
        // Search *.csproj
        var fileList = Directory.GetFiles(".", "*.csproj");
        if (fileList.Length != 1)
            return new AzSyncResponse<object>($"Cannot detect csproj. Found {fileList.Length} csproj files.");

        // read it
        var projectRoot = ProjectRootElement.Open(fileList[0]);
        var secretElement = projectRoot.Properties.FirstOrDefault(a => a.Name == "UserSecretsId");

        // Get Path
        var environmentVariable = Environment.GetEnvironmentVariable("HOME")!;
        var secretPath = $"{environmentVariable}/.microsoft/usersecrets/{secretElement.Value}/secrets.json";

        // Create Directory if not exists.
        var pathName = Path.GetDirectoryName(secretPath)!;
        if (!Directory.Exists(pathName)) Directory.CreateDirectory(pathName);

        // Overwrite
        if (File.Exists(secretPath)) File.Delete(secretPath);
        await using var file = File.OpenWrite(secretPath);
        await using var streamWriter = new StreamWriter(file);
        await streamWriter.WriteAsync(obj);

        // Print out Information
        Console.WriteLine($"Successfully wrote configuration settings to {secretPath}");

        return new AzSyncResponse<object>
        {
            IsError = false
        };
    }
}