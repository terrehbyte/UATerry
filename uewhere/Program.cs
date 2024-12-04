using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Win32;

namespace UATerry
{
    public class UEWhere
    {
        private const string UATSubPath = "Engine\\Build\\BatchFiles\\RunUAT.bat";

        public static Uri GetPathToUProjectFromDirectory(Uri SearchDirectory)
        {
            if (!Directory.Exists(SearchDirectory.LocalPath))
            {
                throw new ArgumentException("Given path is not a directory.");
            }

            string[] CurrentFileNames = Directory.GetFiles(SearchDirectory.LocalPath, "*.uproject", SearchOption.TopDirectoryOnly);

            if (CurrentFileNames.Length == 0)
            {
                throw new Exception("No .uproject file found in given directory.");
            }

            return new Uri(CurrentFileNames[0]);
        }

        public static Uri GetPathToEngineDirectoryFromDirectory(Uri UProjectDirectory)
        {
            if(!Directory.Exists(UProjectDirectory.LocalPath))
            {
                throw new ArgumentException("Given path is not a directory.");
            }

            string[] CurrentFileNames = Directory.GetFiles(UProjectDirectory.LocalPath, "*.uproject", SearchOption.TopDirectoryOnly);

            if (CurrentFileNames.Length == 0)
            {
                throw new Exception("No .uproject file found in given directory.");
            }

            return GetPathToEngineDirectoryFromUProject(new Uri(CurrentFileNames[0]));
        }

        public static Uri GetPathToEngineDirectoryFromUProject(Uri UProjectPath)
        {
            string ProjectFileContents = File.ReadAllText(UProjectPath.LocalPath);

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
            Uri? EngineInstallPath = GetEnginePathFromRegistry(EngineVersion);

            if (EngineInstallPath == null)
            {
                const string BaseProgramFilesPath = "C:\\Program Files\\Epic Games\\UE_{0}\\";
                string ProgramFilesPath = string.Format(BaseProgramFilesPath, EngineVersion);
                if (Directory.Exists(ProgramFilesPath))
                {
                    EngineInstallPath = new Uri(ProgramFilesPath);
                }
                else
                {
                    throw new Exception("Failed to find engine path in registry or default location.");
                }
            }

            string EngineRunUATPath = Path.Join(EngineInstallPath.LocalPath, UATSubPath);
            // Validate engine path
            if (!File.Exists(EngineRunUATPath))
            {
                throw new Exception($"RunUAT.bat was not found at expected path in engine. ({EngineRunUATPath})");
            }

            return EngineInstallPath;
        }

        private static Uri? GetEnginePathFromRegistry(string EngineIdentifier)
        {
            // Look up engine in registry
            const string RegRoot = "HKEY_LOCAL_MACHINE";
            const string BaseKeyPath = "SOFTWARE\\EpicGames\\Unreal Engine\\{0}";
            string RegistryKeyPath = RegRoot + "\\" + string.Format(BaseKeyPath, EngineIdentifier);

            const string EngineValueName = "InstalledDirectory";

            string? EngineInstallPath = (string?)Registry.GetValue(RegistryKeyPath, EngineValueName, null);

            if (EngineInstallPath == null)
            {
                return null;
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
                try
                {
                    UProjectPath = UEWhere.GetPathToEngineDirectoryFromDirectory(new Uri(SearchDirectory)).LocalPath;
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Failed to find .uproject file in given directory.");
                    Console.Error.WriteLine("Error: " + e.Message);
                    return ERROR_PATH_NOT_FOUND;
                }
            }

            string EngineString = UEWhere.GetPathToEngineDirectoryFromUProject(new Uri(UProjectPath)).LocalPath;
            Console.WriteLine(EngineString);

            return 0;
        }
    }
}