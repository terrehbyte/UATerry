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

		static bool UsePerforce = false;

		// 1. Unreal Engine Perforce Configuration (including UseP4Config)
		// 2. P4CONFIG
		// 3. Environment Variables
		static Perforce.P4.Repository? GetP4Repository()
		{
			// get environment variables
			string P4Port = Environment.GetEnvironmentVariable("P4PORT") ?? string.Empty;
			string P4User = Environment.GetEnvironmentVariable("P4USER") ?? string.Empty;
			string P4Client = Environment.GetEnvironmentVariable("P4CLIENT") ?? string.Empty;

			if(UProjectUri == null)
			{
				Console.WriteLine("Cannot resolve Perforce settings; .uproject path not found.");
				return null;
			}

			// get ue settings
			Uri UProjectFileDirectory = new Uri(Path.GetDirectoryName(UProjectUri.LocalPath) + Path.DirectorySeparatorChar);
			Uri RelativeSourceControlSettingsUri = new Uri("Saved\\Config\\WindowsEditor\\SourceControlSettings.ini", UriKind.Relative);
			P4SourceControlSettings Settings = P4SourceControlSettings.GetFromIni(new Uri(UProjectFileDirectory, RelativeSourceControlSettingsUri));

			Perforce.P4.Options Options = new Perforce.P4.Options();
			Options["ProgramName"] = "UATerry";
			//Options["cwd"] = Directory.GetCurrentDirectory() + "\\";
			Options["P4PORT"] = Settings.Port ?? P4Port;
			Options["P4USER"] = Settings.UserName ?? P4User;
			Options["P4CLIENT"] = Settings.Workspace ?? P4Client;

			Perforce.P4.Server Server = new(new Perforce.P4.ServerAddress(Options["P4PORT"]));
			Perforce.P4.Repository Repo = new(Server, false);

			try
			{
				Console.WriteLine("Attempt to connect using the following settings:");
				Console.WriteLine("\tP4PORT: " + Options["P4PORT"]);
				Console.WriteLine("\tP4USER: " + Options["P4USER"]);
				Console.WriteLine("\tP4CLIENT: " + Options["P4CLIENT"]);
				Repo.Connection.Connect(Options);
				Console.Write("Attempting to get client...");
				Repo.Connection.Client = Repo.GetClient(Options["P4CLIENT"]);
				Console.WriteLine(" OK.");
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Failed to connect to Perforce server.");
				Console.Error.WriteLine("Error: " + e.Message + e.StackTrace);
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

			var PerforceOption = new Option<bool>("--perforce", "Use Perforce to checkout files before building");
			PerforceOption.SetDefaultValue(false);

			RootCommand RootCmd = new RootCommand("UATerry locates and runs the Unreal Engine Automation Tool (UAT) with preconfigured parameters for common tasks.");
			RootCmd.AddGlobalOption(PerforceOption);
			RootCmd.SetHandler(() =>
			{
				Console.WriteLine("Nothing to do. Please specify a command.");
				return Task.CompletedTask;
			});

			Command BuildCmd = new Command("build", "Build the project editor");
			BuildCmd.SetHandler(async (bool UsePerforceValue) =>
			{
				UsePerforce = UsePerforceValue;
				await BuildCommand(Args);
			}, PerforceOption);

			RootCmd.AddCommand(BuildCmd);

			return await RootCmd.InvokeAsync(Args);
		}

		static async Task<int> BuildCommand(string[] Args)
		{
			Debug.Assert(EngineUri != null);
			Debug.Assert(UProjectUri != null);

			if (UsePerforce)
			{
				Perforce.P4.Repository? PerforceRepo = GetP4Repository();

				if (PerforceRepo == null)
				{
					Console.WriteLine("Failed to get Perforce repository.");
				}
				// checkout if logged in
				else if (PerforceRepo.Connection.Status == Perforce.P4.ConnectionStatus.Connected)
				{
					Debug.Assert(UProjectDirUri != null);

					string[] RelativePathsToAdd = {
						$"Binaries/Win64/UnrealEditor-{MainModuleName}.dll",
						"Binaries/Win64/UnrealEditor.modules",
						$"Binaries/Win64/{MainModuleName}Editor.target"
					};
					// TODO: We can evaluate the .target file to find all build products for multi-module projects

					bool AddSuccess = true;
					foreach (var RelPath in RelativePathsToAdd)
					{
						try
						{
							PerforceRepo.TryAddEdit(Path.Join(UProjectDirUri.LocalPath, RelPath), Perforce.P4.AddFilesCmdFlags.NoP4Ignore);
							Console.WriteLine($"Marked '{RelPath}' for add/edit in Perforce.");
						}
						catch (Perforce.P4.P4Exception e)
						{
							Console.Error.WriteLine($"Failed to mark '{RelPath}' for add/edit in Perforce.");
							Console.Error.WriteLine("Error: " + e.Message);
							AddSuccess = false;
						}
					}

					if (!AddSuccess)
					{
						return 2;
					}
				}
				else
				{
					Console.WriteLine("Perforce not connected; skipping file checkout.");
				}
			}

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

			return 0;
		}
	}
}
