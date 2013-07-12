using System;
using System.IO;
using System.Linq;
using CommandLine;
using NLog;
using ProcessExec;
using ProcessExec.Client;

namespace HostController
{
    class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        static void Main()
        {
            Console.Clear();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Run();
        }

        static void Run()
        {
            try
            {
                // connect the client (note: for multiple services, we may need to config bindings in code on both ends)
                using (var client = new JobObjectClient("IJobObjectService"))
                {
                    client.Register();
                    client.ProcessExited += (sender, eventArgs) =>
                    {
                        Console.WriteLine("Exit Code: {0}", eventArgs.Value);
                        Environment.Exit(0); // makes sure we exit so service can clean up (assuming a single process)
                    };

                    ListCommands();
                    ListProcesses(client);

                    var prompt = GetPrompt();
                    while (!IsPrompt(prompt, "quit", "q", "exit"))
                    {
                        var arg = GetSecondArg(prompt);

                        if (HasPrompt(prompt, "kill", "k"))
                        {
                            int pid;
                            if (Int32.TryParse(arg, out pid))
                            {
                                client.StopProcess(pid);
                            }
                        }
                        else if (HasPrompt(prompt, "run", "r"))
                        {
                            var runOptions = GetOptions(arg);
                            client.StartProcess(runOptions.Command, runOptions.WorkingDirectory, runOptions.Arguments);
                        }
                        else if (HasPrompt(prompt, "list", "l"))
                        {
                            ListProcesses(client);
                        }
                        else if (HasPrompt(prompt, "help", "h"))
                        {
                            ListCommands();
                        }

                        prompt = GetPrompt();
                    }
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("Error in run loop", ex);
            }
        }

        private static RunOptions GetOptions(string args)
        {
            var runOptions = new RunOptions();
            try
            {
                Parser.Default.ParseArguments(args.Split(new[] {' '}, 3), runOptions);
            }
            catch { }

            return runOptions;
        }

        private static string GetPrompt()
        {
            Console.Write("Please enter a comamnd: ");
            return Console.ReadLine();
        }

        private static bool IsPrompt(string prompt, params string[] matches)
        {
            return matches.Any(m => prompt.Equals(m, StringComparison.OrdinalIgnoreCase));
        }

        private static bool HasPrompt(string prompt, params string[] matches)
        {
            return matches.Any(m => prompt.StartsWith(m, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetSecondArg(string prompt)
        {
            try
            {
                return prompt.Split(new[] {' '}, 2)[1];
            }
            catch
            {
                return String.Empty;
            }
        }

        private static void ListProcesses(IJobObjectClient client)
        {
            Console.WriteLine();
            var procs = client.ListProcesses();
            Console.WriteLine("Process List:");
            Console.WriteLine("----------------------------------------------------------");
            foreach (var process in procs)
            {
                Console.WriteLine("Process {0}:{1}", process.ID, process.Name);
            }
            Console.WriteLine();
        }

        private static void ListCommands()
        {
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("----------------------------------------------------------");
            Console.WriteLine("help (h)       - lists commands");
            Console.WriteLine("quit (q)       - quit or exit");
            Console.WriteLine("list (l)       - lists the currently hosted processes");
            Console.WriteLine("kill (k) pid   - Kills the process having specified pid");
            Console.WriteLine(@"run command=""cmd.exe"" wd=""c:\foo\bar"" args=""dir *.*""");
            //Console.WriteLine("send cmd       - where cmd is a command to send to the process stdin");
            Console.WriteLine();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var genericMessage = String.Format("Unhandled exception in AppDomain '{0}'", AppDomain.CurrentDomain.FriendlyName);
            var ex = e.ExceptionObject as Exception;

            if (ex != null)
            {
                EventLogger.HostController.Error(genericMessage, ex);
            }
            else if (e.ExceptionObject != null)
            {
                EventLogger.HostController.Error(genericMessage, e.ExceptionObject);
            }
            else
            {
                EventLogger.HostController.Error(genericMessage);
            }
        }

        private class RunOptions
        {
            [Option("wd", Required = false, DefaultValue = "", HelpText = "This is the working directory")]
            public string WorkingDirectory { get; set; }

            [Option("command", Required = true, DefaultValue = "cmd.exe", HelpText = "This is the command to execute e.g.( cmd.exe )")]
            public string Command { get; set; }

            [Option("args", Required = true, DefaultValue = "echo hello world", HelpText = "This is the args passed to the command e.g.( dir *.* )")]
            public string Arguments { get; set; }
        }
    }
}
