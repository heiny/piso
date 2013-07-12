using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using NLog;
using ProcessExec;
using ProcessExec.Client;
using ProcessExec.Service;

namespace HostDriver
{
    class Program
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private static readonly ManualResetEvent exitLatch = new ManualResetEvent(false);

        private static ProcessHostManager serviceManager;
        private static JobObjectClient client;
        private static Task runTask;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            var options = GetOptions(args);

            if (options == null)
            {
                Console.WriteLine("Invalid arguments");
                Environment.Exit(1);
            }

            runTask = Task.Run(() => Run(options));
            Console.ReadLine();

            exitLatch.Set(); // exit, stop waiting on process

            try
            {
                runTask.Wait(TimeSpan.FromSeconds(30)); // wait for things to cleanup
            }
            catch (AggregateException ex)
            {
                log.ErrorException("Error", ex.Flatten());
            }
            finally
            {
                Cleanup();
            }
        }

        private static void Run(Options options)
        {
            var processDir = Path.Combine(Environment.CurrentDirectory, "ProcessHost"); // \process-exec\HostDriver\bin\debug\ProcessHost

            try
            {
                var processCred = new NetworkCredential(options.UserName, options.Password);
                serviceManager = new ProcessHostManager(processDir, options.ContainerDir, "processhost.exe", options.ServiceName, processCred);
                serviceManager.RunService();

                // connect the client (note: for multiple services, we may need to config bindings in code on both ends)
                client = new JobObjectClient("IJobObjectService");
                client.ServiceMessageReceived += (sender, eventArgs) => log.Info(eventArgs.Value);
                client.OutputReceived += (sender, eventArgs) => log.Info(eventArgs.Value);
                client.ErrorReceived += (sender, eventArgs) => log.Error(eventArgs.Value);
                client.ProcessExited += (sender, eventargs) => log.Info("Process Exit: {0}", eventargs.Value);

                client.Register();
                client.SetJobLimits(new JobObjectLimits
                {
                    MemoryMB = options.MemoryLimit,
                    CpuPercent = options.CpuRateLimit
                });

                client.StartProcess(options.Command, options.WorkingDirectory, options.Arguments);
                
                //client.StartProcess(@"c:\tmp\eatcpu.exe", @"C:\tmp", string.Empty);
                //client.StartProcess(@"c:\tmp\eatmemory.exe", @"C:\tmp", "512 10"); // 512 mb, 10 seconds wait for exit
                //client.StartProcess("cmd.exe", @"C:\tmp", "/c dir *.*");
                //client.StartProcess("cmd.exe", String.Empty, "/c ping 127.0.0.1 -n 200 -w 1000");
                //client.StartProcess("cmd.exe", String.Empty, "/c ping 127.0.0.1 -n 25 -w 500");

                exitLatch.WaitOne(); // wait for exit
                Console.WriteLine("Press [enter] to terminate and cleanup"); // reminder
            }
            catch (Exception ex)
            {
                log.ErrorException("Error in run loop", ex);
            }
            finally
            {
                Cleanup();
            }
        }

        private static Options GetOptions(string[] args)
        {
            var options = new Options();
            try
            {
                if (!Parser.Default.ParseArguments(args, options))
                {
                    return null;
                }
            }
            catch(Exception ex)
            {
                log.ErrorException("Unable to parse options", ex);
                return null;
            }

            return options;
        }

        private static void Cleanup()
        {
            try
            {
                if (client != null)
                {
                    client.Dispose();
                    client = null;
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("Unable to cleanup client", ex);
            }

            try
            {
                if (serviceManager != null)
                {
                    serviceManager.Dispose();
                    serviceManager = null;
                }
            }
            catch (Exception ex)
            {
                log.ErrorException("Unable to cleanup service", ex);
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var genericMessage = String.Format("Unhandled exception in AppDomain '{0}'", AppDomain.CurrentDomain.FriendlyName);
            var ex = e.ExceptionObject as Exception;

            if (ex != null)
            {
                EventLogger.HostDriver.Error(genericMessage, ex);
            }
            else if (e.ExceptionObject != null)
            {
                EventLogger.HostDriver.Error(genericMessage, e.ExceptionObject);
            }
            else
            {
                EventLogger.HostDriver.Error(genericMessage);
            }
        }

        private class Options
        {
            [Option("UserName", Required = false, DefaultValue = @"BH-E6520\test_user", HelpText = "This is the username to run the service as")]
            public string UserName { get; set; }

            [Option("Password", Required = false, DefaultValue = "Pass@word1", HelpText = "This is the password for the service user")]
            public string Password { get; set; }

            [Option("ContainerDir", Required = false, DefaultValue = @"C:\IronFoundry\warden\containers", HelpText = "This is the directory the service is installed to")]
            public string ContainerDir { get; set; }

            [Option("ServiceName", Required = false, DefaultValue = "TST_SVC", HelpText = "This is the name of the service")]
            public string ServiceName { get; set; }

            [Option("CpuRateLimit", Required = false, DefaultValue = (byte)0, HelpText = "This is the CPU rate limit")]
            public byte CpuRateLimit { get; set; }

            [Option("MemoryLimit", Required = false, DefaultValue =(uint)0, HelpText = "This is the memory limit in MB")]
            public uint MemoryLimit { get; set; }

            [Option("Command", Required = false, DefaultValue = "cmd.exe", HelpText = "This is the command to execute e.g.( cmd.exe )")]
            public string Command { get; set; }

            [Option("Arguments", Required = false, DefaultValue = "/c ping 127.0.0.1 -n 10 -w 100", HelpText = "This is the args passed to the command e.g.( dir *.* )")]
            public string Arguments { get; set; }

            [Option("WorkingDirectory", Required = false, HelpText = "This is the working directory")]
            public string WorkingDirectory { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }
    }
}
