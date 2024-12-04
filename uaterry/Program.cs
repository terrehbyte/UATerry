using System.Diagnostics;
using System.CommandLine;

namespace UATerry
{
    internal class Program
    {
        static Uri? UProjectUri;
        static Uri? EngineUri;
        const string UATSubPath = "Engine\\Build\\BatchFiles\\RunUAT.bat";

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
                EngineUri = UEWhere.GetPathToEngineDirectoryFromUProject(UProjectUri);
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
            StartInfo.Arguments = $"BuildEditor -project=\"{UProjectUri}\" -notools";
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

            return 0;
        }
    }
}