using System.Diagnostics;

namespace UATerry
{
    internal class P4CLI
    {
        public string WorkingDirectory;
        public string P4Path = "p4.exe";

        public P4CLI()
        {
            WorkingDirectory = Directory.GetCurrentDirectory();
        }

        // Login Status
        public bool LoginStatus
        {
            get
            {
                return ExecuteP4Subcommand("login", "-s").Equals(0);
            }
        }

        // Sync (aka Get Revision)
        public void Sync(string PathSpec, string ExtraArgs = "") => ExecuteP4Subcommand("sync", ExtraArgs + " " + PathSpec);

        // Clean
        public void Clean(string PathSpec, string ExtraArgs = "") => ExecuteP4Subcommand("clean", ExtraArgs + " " + PathSpec);

        // Checkout
        public void Edit(string PathSpec, string ExtraArgs = "") => ExecuteP4Subcommand("edit", ExtraArgs + " " + PathSpec);

        public void Add(string PathSpec, string ExtraArgs = "") => ExecuteP4Subcommand("add", ExtraArgs + " " + PathSpec);

        public void TryAddEdit(string PathSpec, string ExtraFilesArgs = "", string ExtraAddArgs = "", string ExtraEditArgs = "")
        {
            if (Files(PathSpec, ExtraFilesArgs))
            {
                Edit(PathSpec, ExtraEditArgs);
            }
            else
            {
                Add(PathSpec, ExtraAddArgs);
            }
        }

        public bool Files(string PathSpec, string ExtraArgs = "")
        {
            bool Success = true;

            ExecuteP4Subcommand("files", PathSpec + " " + ExtraArgs, (sender, e) => {
                string? OutputString = e.Data;
                if (OutputString != null)
                {
                    if(OutputString.StartsWith("error", StringComparison.InvariantCultureIgnoreCase) || OutputString.StartsWith("warning", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Success = false;
                    }
                }
            });

            return Success;
        }

        private int ExecuteP4Subcommand(string Subcommand, string Arguments, DataReceivedEventHandler? OutputHandler = null, DataReceivedEventHandler? ErrorHandler = null)
        {
            ProcessStartInfo StartInfo = new ProcessStartInfo();
            StartInfo.FileName = P4Path;
            StartInfo.Arguments = $"-s {Subcommand} {Arguments}";
            StartInfo.WorkingDirectory = WorkingDirectory;
            StartInfo.UseShellExecute = false;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError = true;

            Process BuildProcess = new Process();
            BuildProcess.StartInfo = StartInfo;
            BuildProcess.OutputDataReceived += OutputHandler == null ? (sender, e) => Console.WriteLine(e.Data) : OutputHandler;
            BuildProcess.ErrorDataReceived += ErrorHandler == null ? (sender, e) => Console.Error.WriteLine(e.Data) : ErrorHandler;
            BuildProcess.Start();
            BuildProcess.BeginOutputReadLine();
            BuildProcess.BeginErrorReadLine();
            BuildProcess.WaitForExit();

            return BuildProcess.ExitCode;
        }
    }
}