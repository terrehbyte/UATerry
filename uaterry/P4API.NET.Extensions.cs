using Perforce.P4;

public static class P4APINetExtensions
{
	// Try AddEdit
	public static bool TryAddEdit(this Repository Repository, string PathSpec, AddFilesCmdFlags AddFlags = AddFilesCmdFlags.None, EditFilesCmdFlags EditFlags = EditFilesCmdFlags.None)
	{
		// Check if file is already in Perforce
		FileSpec FileSpec = new FileSpec(new LocalPath(PathSpec), null);
		IList<Perforce.P4.File>? FilesResults = Repository.GetFiles(null, FileSpec);

		// If so, edit it
		if (FilesResults != null && FilesResults.Any())
		{
			EditCmdOptions EditOptions = new EditCmdOptions(EditFlags, 0, null);
			var Result = Repository.Connection.Client.EditFiles(EditOptions, FileSpec);
			return Result != null && Result.Any();
		}
		// If not, add it
		else
		{
			AddFilesCmdOptions AddOptions = new AddFilesCmdOptions(AddFlags, 0, null);
			var Result = Repository.Connection.Client.AddFiles(AddOptions, FileSpec);
			return Result != null && Result.Any();
		}
	}
}
