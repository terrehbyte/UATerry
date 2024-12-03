using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Win32;

namespace UATerry
{
    public class UEWhere
    {
        public static Uri GetPathToEngineDirectoryFromUProject(Uri UProjectPath)
        {
            string ProjectFileContents = File.ReadAllText(UProjectPath.AbsolutePath);

            string? EngineVersion = null;

            // Extract the expected engine version from the .uproject file
            try
            {
                JsonNode? ProjectNode = JsonNode.Parse(ProjectFileContents);
                if (ProjectNode == null)
                {
                    throw new ArgumentException(".uproject file was a JSON null.");
                }

                JsonObject ProjectObject = ProjectNode.AsObject();
                EngineVersion = (string?)ProjectObject["EngineAssociation"];
                if (EngineVersion == null)
                {
                    throw new ArgumentException("Failed to extract engine version from .uproject file.");
                }
            }
            catch (JsonException)
            {
                throw new JsonException("Failed to parse .uproject file.");
            }
            catch (InvalidCastException)
            {
                throw new JsonException("Failed to parse. EngineAssociation value in .uproject file was not a string.");
            }

            // Look up engine in registry
            const string RegRoot = "HKEY_LOCAL_MACHINE";
            const string BaseKeyPath = "SOFTWARE\\EpicGames\\Unreal Engine\\{0}";
            string RegistryKeyPath = RegRoot + "\\" + string.Format(BaseKeyPath, EngineVersion);

            const string EngineValueName = "InstalledDirectory";

            string? EngineInstallPath = (string?)Registry.GetValue(RegistryKeyPath, EngineValueName, null);

            if (EngineInstallPath == null)
            {
                throw new Exception($"Expected registry entry was not found at {RegistryKeyPath}.");
            }

            return new Uri(EngineInstallPath);
        }
    }

    internal class Program
    {
        static int Main(string[] Args)
        {
            const int ERROR_PATH_NOT_FOUND = 2;
            const int ERROR_JSON_PARSE = 10;
            const int ERROR_JSON_NULL = 11;
            const int ERROR_JSON_EXTRACT = 12;
            const int ERROR_JSON_UNKNOWN = 19;
            const int ERROR_REG_LOOKUP = 20;

            string UProjectPath = string.Empty;
            string SearchDirectory = Directory.GetCurrentDirectory();

            // use directory given
            if (Args.Length >= 1)
            {
                // is it a file that ends in .uproject?
                if (string.Compare(Path.GetExtension(Args[0]), ".uproject", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    UProjectPath = Args[0];
                }
                else
                {
                    // if not, is it a directory?
                    if (!Directory.Exists(Args[0]))
                    {
                        Console.Error.WriteLine("Given path is not a .uproject file or a directory.");
                        return ERROR_PATH_NOT_FOUND;
                    }

                    SearchDirectory = Args[0];
                }
            }

            if (UProjectPath == string.Empty)
            {
                string[] CurrentFileNames = Directory.GetFiles(SearchDirectory, "*.uproject", SearchOption.TopDirectoryOnly);

                if (CurrentFileNames.Length == 0)
                {
                    Console.Error.WriteLine("No .uproject file found in given directory.");
                    return ERROR_PATH_NOT_FOUND;
                }

                UProjectPath = CurrentFileNames[0];
            }

            Uri EngineUri = UEWhere.GetPathToEngineDirectoryFromUProject(new Uri(UProjectPath));
            Console.WriteLine(EngineUri.AbsolutePath);

            return 0;
        }
    }
}