using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CmdLine
{
    public class CommandLine : IDisposable
    {
        //If print is not passed to Run we default to use Print
        public bool Print { get; set; }
        private bool curPrint;

        Process process;
        StreamWriter STDIN;
        StreamReader STDOUT;
        StreamReader STDERR;

        Task currentTask;
        CancellationTokenSource taskCancel = new CancellationTokenSource();

        public CommandLine(bool Print = true, bool createNoWindow=false)
        {
            this.Print = Print;

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.FileName = "cmd.exe";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = createNoWindow;

            process = Process.Start(startInfo);
            STDIN = process.StandardInput;
            STDOUT = process.StandardOutput;
            STDERR = process.StandardError;

            Task.Run(async () => await ReadLoop(process));
            Task.Run(async () => await ErrReadLoop(process));

            //Run an ls to clear out some of the buffers, or something?
            Run("ls");
        }

        public void RunAsTask(string command, Action<string> onOutput = null, bool? print = null)
        {
            currentTask = Task.Run(() =>
            {
                var outputs = RunEnumerable(command, print);

                try
                {
                    foreach (var output in outputs)
                    {
                        if (onOutput != null)
                        {
                            onOutput(output);
                        }
                    }
                }
                catch (OperationCanceledException) { }
            });
            //Who can access this first?
            //(RACE CONDITION)
            currentTask = null;
        }

        public List<string> Run(string command, bool? print = null)
        {
            return RunEnumerable(command, print).ToList();
        }

        public IEnumerable<string> RunEnumerable(string command, bool? print = null)
        {
            if (currentTask != null)
            {
                currentTask.Wait();
            }

            if (!print.HasValue)
            {
                print = Print;
            }

            curPrint = print.Value;

            BlockingCollection<string> output = new BlockingCollection<string>();

            procOutput = output;

            STDIN.WriteLine(command);
            STDIN.WriteLine();

            return output.GetConsumingEnumerable(taskCancel.Token);
        }

        ManualResetEvent readExited = new ManualResetEvent(false);
        ManualResetEvent errReadExited = new ManualResetEvent(false);
        //If not set we assume no output is required
        BlockingCollection<string> procOutput;
        async Task ReadLoop(Process process)
        {
            while (!STDOUT.EndOfStream)
            {
                string line = await STDOUT.ReadLineAsync();

                string cmdStr = "\\w:(\\\\[^<>:\"/\\|\\?\\*]+)*>";
                bool isCommandStart = new Regex(cmdStr + ".+$").IsMatch(line);
                bool isCommandEnd = new Regex(cmdStr + "$").IsMatch(line);

                if (isCommandStart)
                {
                    if (curPrint)
                    {
                        Console.WriteLine(line);
                        Console.WriteLine("---- COMMAND START ----");
                    }
                }
                else if (isCommandEnd)
                {
                    if (curPrint)
                    {
                        Console.WriteLine("---- COMMAND DONE ----");
                        Console.WriteLine(line);
                    }
                    if (procOutput != null)
                    {
                        procOutput.CompleteAdding();
                        procOutput = null;
                    }
                }
                else
                {
                    if (curPrint)
                    {
                        Console.WriteLine(line);
                    }
                    if (procOutput != null)
                    {
                        procOutput.Add(line);
                    }
                }
            }
            readExited.Set();
        }

        async Task ErrReadLoop(Process process)
        {
            while (!STDERR.EndOfStream)
            {
                string line = await STDERR.ReadLineAsync();
                Console.WriteLine("ERROR: " + line);
            }
            readExited.Set();
        }

        public void Dispose()
        {
            process.Close();
            readExited.WaitOne();
            errReadExited.WaitOne();
        }
    }

}
