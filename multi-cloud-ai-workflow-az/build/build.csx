#load "task.csx"

using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using Newtonsoft.Json.Linq;

public static class Build
{
    public static string RootDir { get; }

    public static dynamic Dirs { get; } = new ExpandoObject();

    public static IDictionary<string, IBuildTask> Tasks { get; } = new Dictionary<string, IBuildTask>();

    public static dynamic Inputs { get; private set; } = new ExpandoObject();

    public static DateTime Timestamp { get; } = DateTime.UtcNow;

    public static string TimestampFileFormatted => Timestamp.ToString("yyyyMMddhhmmssfff");

    static Build()
    {
        RootDir = Directory.GetCurrentDirectory();
    }

    public static void ReadInputs(string inputFilePath = "build-inputs.json", IDictionary<string, string> defaults = null)
    {
        IDictionary<string, object> inputs = Inputs;

        var jObj = JObject.Parse(File.ReadAllText(inputFilePath));
        
        foreach (var prop in jObj.Properties())
            inputs[prop.Name] = prop.Value.Value<string>();

        if (defaults != null)
            foreach (var def in defaults)
                if (!inputs.ContainsKey(def.Key))
                    inputs[def.Key] = def.Value;
    }

    public static async Task Run(string name)
    {
        if (name == null)
        {
            Console.Error.WriteLine("Please specify a build task.");
            return;
        }

        Console.WriteLine("Executing MCMA build...");
        if (Tasks.ContainsKey(name))
        {
            var started = DateTime.Now;
            string error = null;
            try
            {
                await Tasks[name].Run();
            }
            catch (Exception ex)
            {
                error = ex.ToString();
                Console.Error.WriteLine(error);
            }
            var ended = DateTime.Now;

            Console.WriteLine();
            Console.WriteLine("---------------------------------------------------------");

            var origColor = Console.ForegroundColor;
            Console.ForegroundColor = error != null ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine($"Build {(error != null ? "FAILED" : "succeeded")}");
            Console.ForegroundColor = origColor;

            Console.WriteLine($"Started: {started}");
            Console.WriteLine($"Ended: {ended}");
            Console.WriteLine($"Time to build: {ended - started}");
            Console.WriteLine("---------------------------------------------------------");
            Console.WriteLine();
        }
        else
            Console.Error.WriteLine($"Unknown task '{name}'");
    }
}