using System;
using Microsoft.Extensions.Configuration;

namespace UATerry
{
	internal struct P4SourceControlSettings
	{
		public bool? UseP4Config { get; set; }
		public string? Port { get; set; }
		public string? UserName { get; set; }
		public string? Workspace { get; set; }

		public static P4SourceControlSettings GetFromIni(System.Uri IniPath)
		{
			// Build a configuration object from INI file
			IConfiguration Config = new ConfigurationBuilder()
				.AddIniFile(IniPath.LocalPath)
				.Build();

			// Read configuration values
			P4SourceControlSettings Settings = new P4SourceControlSettings();

			// Get a configuration section
			IConfigurationSection ProviderSection = Config.GetSection("SourceControl.SourceControlSettings");
			if (ProviderSection.Exists())
			{
				string? ProviderName = ProviderSection.GetValue<string?>("Provider");
				if(string.Compare(ProviderName, "Perforce", StringComparison.InvariantCultureIgnoreCase) != 0)
				{
					return Settings;
				}
			}

			// Get a configuration section
			IConfigurationSection PerforceSection = Config.GetSection("PerforceSourceControl.PerforceSourceControlSettings");
			if (PerforceSection.Exists())
			{
				Settings = PerforceSection.Get<P4SourceControlSettings>();
			}

			return Settings;
		}
	}
}
