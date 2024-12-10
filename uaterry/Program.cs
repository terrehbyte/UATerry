using System.Diagnostics;
using System.CommandLine;

namespace UATerry
{
    internal class Program
    {
        static Uri? UProjectUri;
        static Uri? UProjectDirUri;
        static Uri? EngineUri;

        static string MainModuleName = string.Empty;
        const string UATSubPath = "Engine\\Build\\BatchFiles\\RunUAT.bat";

        static Perforce.P4.Repository GetP4Repository()
        {
            // get environment variables
            string? P4Port = Environment.GetEnvironmentVariable("P4PORT");
            // string? P4User = Environment.GetEnvironmentVariable("P4USER");
            // string? P4Client = Environment.GetEnvironmentVariable("P4CLIENT");

            Perforce.P4.Options Options = new Perforce.P4.Options();
            Options["ProgramName"] = "UATerry";
            Options["cwd"] = Directory.GetCurrentDirectory();

            Perforce.P4.Server Server = new(new Perforce.P4.ServerAddress(P4Port));
            Perforce.P4.Repository Repo = new(Server);

            try
            {
                Repo.Connection.Connect(Options);
            }
            catch (Perforce.P4.P4Exception e)
            {
                Console.Error.WriteLine("Failed to connect to Perforce server.");
                Console.Error.WriteLine("Error: " + e.Message);
            }

            return Repo;
        }

        static async Task<int> Main(string[] Args)
        {
            // get working directory
            string WorkingDirectory = Directory.GetCurrentDirectory();

            if (Args.Length == 0)
            {
                Console.WriteLine("Nothing to do.");
                return 0;
            }

            try
            {
                UProjectUri = UEWhere.GetPathToUProjectFromDirectory(new Uri(WorkingDirectory));
                UProjectDirUri = new Uri(Path.GetDirectoryName(UProjectUri.LocalPath)!);
                EngineUri = UEWhere.GetPathToEngineDirectoryFromUProject(UProjectUri);
                MainModuleName = UEWhere.ModuleName;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Failed to find .uproject file in given directory.");
                Console.Error.WriteLine("Error: " + e.Message);
                return 1;
            }

            RootCommand RootCmd = new RootCommand("Sample root command");
            RootCmd.SetHandler(() =>
            {
                Console.WriteLine("Hello from root command");
                return Task.CompletedTask;
            });

            Command BuildCmd = new Command("build", "Build the project editor");
            BuildCmd.SetHandler(() => BuildCommand(Args));

            RootCmd.AddCommand(BuildCmd);

            return await RootCmd.InvokeAsync(Args);
        }

        static async Task<int> BuildCommand(string[] Args)
        {
            Debug.Assert(EngineUri != null);
            Debug.Assert(UProjectUri != null);

            // launch build
            ProcessStartInfo StartInfo = new ProcessStartInfo();
            StartInfo.FileName = Path.Join(EngineUri.LocalPath, UATSubPath);
            StartInfo.Arguments = $"BuildEditor -project=\"{UProjectUri.LocalPath}\" -notools";
            StartInfo.WorkingDirectory = EngineUri.LocalPath;
            StartInfo.UseShellExecute = false;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError = true;

            Process BuildProcess = new Process();
            BuildProcess.StartInfo = StartInfo;
            BuildProcess.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            BuildProcess.ErrorDataReceived += (sender, e) => Console.Error.WriteLine(e.Data);
            BuildProcess.Start();
            BuildProcess.BeginOutputReadLine();
            BuildProcess.BeginErrorReadLine();
            BuildProcess.WaitForExit();

            Perforce.P4.Repository PerforceRepo = GetP4Repository();

            // checkout if logged in
            if (PerforceRepo.Connection.Status == Perforce.P4.ConnectionStatus.Connected)
            {
                Debug.Assert(UProjectDirUri != null);

                try
                {
                    PerforceRepo.TryAddEdit(Path.Join(UProjectDirUri.LocalPath, $"Binaries/Win64/UnrealEditor-{MainModuleName}.dll"), Perforce.P4.AddFilesCmdFlags.NoP4Ignore);
                    PerforceRepo.TryAddEdit(Path.Join(UProjectDirUri.LocalPath, "Binaries/Win64/UnrealEditor.modules"), Perforce.P4.AddFilesCmdFlags.NoP4Ignore);
                    PerforceRepo.TryAddEdit(Path.Join(UProjectDirUri.LocalPath, $"Binaries/Win64/{MainModuleName}Editor.target"), Perforce.P4.AddFilesCmdFlags.NoP4Ignore);
                    // TODO: We can evaluate the .target file to find all build products
                }
                catch (Perforce.P4.P4Exception e)
                {
                    Console.Error.WriteLine("Failed to add or edit files in Perforce.");
                    Console.Error.WriteLine("Error: " + e.Message);
                    return 2;
                }
            }

            return 0;
        }

        // static async Task<int> SetupCommand()
        // {
        //     // check for expected executables on path

        //     // set p4 ignore if not defined
        // }
    }
}