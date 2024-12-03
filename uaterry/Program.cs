using System.Diagnostics;

namespace UATerry
{
    internal class Program
    {
        static int Main(string[] Args)
        {
            // get working directory
            string WorkingDirectory = Directory.GetCurrentDirectory();

            const string UATSubPath = "Engine\\Build\\BatchFiles\\RunUAT.bat";

            if (Args.Length == 0)
            {
                Console.WriteLine("Nothing to do.");
                return 0;
            }

            if (string.Compare(Args[0], "build", StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                Console.Error.WriteLine("Unknown command");
                return 1;
            }

            Uri UProjectPath = UEWhere.GetPathToUProjectFromDirectory(new Uri(WorkingDirectory));
            Uri EnginePath = UEWhere.GetPathToEngineDirectoryFromUProject(UProjectPath);

            // launch build
            ProcessStartInfo StartInfo = new ProcessStartInfo();
            StartInfo.FileName = Path.Combine(EnginePath.AbsolutePath, UATSubPath);
            StartInfo.Arguments = $"BuildEditor -project=\"{UProjectPath.AbsolutePath}\" -notools";
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