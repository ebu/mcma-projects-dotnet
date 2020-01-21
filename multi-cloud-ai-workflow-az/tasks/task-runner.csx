#load "task.csx"
#load "aggregate-task.csx"

using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

public static class TaskRunner
{
    public const string DefaultInputsFile = "task-inputs.json";

    public static string RootDir { get; }

    public static dynamic Dirs { get; } = new ExpandoObject();

    public static IDictionary<string, ITask> Tasks { get; } = new Dictionary<string, ITask>();

    public static string InputsFile { get; private set; }

    public static dynamic Inputs { get; private set; } = new ExpandoObject();

    public static DateTime Timestamp { get; } = DateTime.UtcNow;

    public static string TimestampFileFormatted => Timestamp.ToString("yyyyMMddhhmmssfff");

    static TaskRunner()
    {
        RootDir = Directory.GetCurrentDirectory();
    }

    public static void ReadInputs(string inputFilePath = DefaultInputsFile, IDictionary<string, string> defaults = null)
    {
        InputsFile = inputFilePath;

        IDictionary<string, object> inputs = Inputs;

        if (!File.Exists(InputsFile))
            return;

        var jObj = JObject.Parse(File.ReadAllText(InputsFile));
        
        foreach (var prop in jObj.Properties())
            inputs[prop.Name] = prop.Value.Value<string>();

        if (defaults != null)
            foreach (var def in defaults)
                if (!inputs.ContainsKey(def.Key))
                    inputs[def.Key] = def.Value;
    }

    public static void AddRuntimeInputs(params (string, string)[] additionalInputs)
    {
        IDictionary<string, object> inputs = Inputs;

        foreach (var (name, value) in additionalInputs)
            inputs[name] = value;
    }

    public static async Task Run(string name, ITask startWith = null, ITask endWith = null)
    {
        if (name == null)
        {
            Console.Error.WriteLine("Please specify a task.");
            return;
        }

        Console.WriteLine("Executing MCMA tasks...");
        if (Tasks.ContainsKey(name))
        {
            var tasks = new List<ITask>();
            
            if (startWith != null)
                tasks.Add(startWith);
            tasks.Add(Tasks[name]);
            if (endWith != null)
                tasks.Add(endWith);

            var aggTask = new AggregateTask(tasks.ToArray());

            var started = DateTime.Now;
            string error = null;
            try
            {
                await aggTask.Run();
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