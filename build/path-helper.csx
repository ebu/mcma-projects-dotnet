
public static class PathHelper
{
    public static string Where(string executable)
    {
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (pathVar == null)
            throw new Exception($"PATH variable not set.");

        var paths = pathVar.Split(';');

        foreach (var path in paths)
        {
            var exePath = Path.Combine(path, executable + ".exe");
            var cmdPath = Path.Combine(path, executable + ".cmd");
            var batPath = Path.Combine(path, executable + ".bat");

            if (File.Exists(exePath))
                return exePath;
            else if (File.Exists(cmdPath))
                return cmdPath;
            else if (File.Exists(batPath))
                return batPath;
        }

        throw new Exception($"An executable with name '{executable}' was not found in any of the directories in the PATH variable.");
    }
}