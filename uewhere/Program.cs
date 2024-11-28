using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Win32;

const int ERROR_FILE_NOT_FOUND = 2;
const int ERROR_JSON_PARSE = 10;
const int ERROR_JSON_NULL = 11;
const int ERROR_JSON_EXTRACT = 12;
const int ERROR_JSON_UNKNOWN = 19;
const int ERROR_REG_LOOKUP = 20;

string SearchDirectory = Directory.GetCurrentDirectory();

string[] Args = Environment.GetCommandLineArgs();
if (Args.Length >= 2)
{
    SearchDirectory = Args[1];
}

string[] CurrentFileNames = Directory.GetFiles(SearchDirectory, "*.uproject", SearchOption.TopDirectoryOnly);

if (CurrentFileNames.Length == 0)
{
    Console.Error.WriteLine("No .uproject file found in given directory.");
    return ERROR_FILE_NOT_FOUND;
}

string ProjectFileName = CurrentFileNames[0];
string ProjectFileContents = File.ReadAllText(ProjectFileName);

string? EngineVersion = null;

// Extract the expected engine version from the .uproject file
try
{
    JsonNode? ProjectNode = JsonNode.Parse(ProjectFileContents);
    if (ProjectNode == null)
    {
        Console.Error.WriteLine(".uproject file was a JSON null.");
        return ERROR_JSON_NULL;
    }

    JsonObject ProjectObject = ProjectNode.AsObject();
    EngineVersion = (string?)ProjectObject["EngineAssociation"];
    if (EngineVersion == null)
    {
        Console.Error.WriteLine("Failed to extract engine version from .uproject file.");
        return ERROR_JSON_EXTRACT;
    }
}
catch (JsonException)
{
    Console.Error.WriteLine("Failed to parse .uproject file.");
    return ERROR_JSON_PARSE;
}
catch (InvalidCastException)
{
    Console.Error.WriteLine("Failed to parse. EngineAssociation value in .uproject file was not a string.");
    return ERROR_JSON_PARSE;
}
catch (NullReferenceException)
{
    Console.Error.WriteLine("Unknown error while trying to extract engine version from .uproject file.");
    return ERROR_JSON_UNKNOWN;
}

// Look up engine in registry
const string RegRoot = "HKEY_LOCAL_MACHINE";
const string BaseKeyPath = "SOFTWARE\\EpicGames\\Unreal Engine\\{0}";
string RegistryKeyPath = RegRoot + "\\" + string.Format(BaseKeyPath, EngineVersion);

const string EngineValueName = "InstalledDirectory";

string? EngineInstallPath = (string?)Registry.GetValue(RegistryKeyPath, EngineValueName, null);

if (EngineInstallPath == null)
{
    Console.Error.WriteLine($"Expected registry entry was not found at {RegistryKeyPath}.");
    return ERROR_REG_LOOKUP;
}

// Write path to engine to stdout
Console.WriteLine(EngineInstallPath);

return 0;
