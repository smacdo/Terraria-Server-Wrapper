using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scott.Terraria.Server
{
    /// <summary>
    ///  Represents the current state of a terraria server.
    /// </summary>
    public enum ServerState
    {
        None,
        Starting,
        Ready,
        Saving
    }

    /// <summary>
    ///  Responsible for configuring, running and monitoring a Terraria server process.
    /// </summary>
    public class TerrariaServerProcess
    {
        private Process mProcess;

        public bool HasExited { get { return mProcess.HasExited; } }

        /// <summary>
        ///  Get or set the name of the server config file. This should be in the working directory.
        /// </summary>
        public string ConfigFileName { get; set; }

        /// <summary>
        ///  Get the current state of the terraria server.
        /// </summary>
        public ServerState CurrentState { get; private set; }

        /// <summary>
        ///  Get if the server is idle.
        /// </summary>
        /// <remarks>
        ///  An idle terraria server means that the server is not printing output but is waiting for
        ///  commands.
        /// </remarks>
        public bool IsReadyForCommands
        {
            get { return CurrentState == ServerState.Ready; }
        }

        public bool IsRunning
        {
            get { return mProcess != null && !mProcess.HasExited; }
        }

        /// <summary>
        ///  Get or set the path to the terraria server executable.
        /// </summary>
        public string ServerExePath { get; set; }

        /// <summary>
        ///  Get or set the path to the working directory. This should be the directory that the terraria server exe
        ///  is located in.
        /// </summary>
        public string WorkingDirectory { get; set; }

        public void Start()
        {
            // Create a process object that will run the terraria server, and intercept it's input/output.
            // Start the process up before attempting to read or write to it.
            mProcess = CreateServerProcessObject();
            mProcess.Start();

            // Get a stream writer we can use to write to the server process's standard input.
            var serverInputStream = mProcess.StandardInput;

            // Start reading asynchronously from the process.
            mProcess.BeginOutputReadLine();
            mProcess.BeginErrorReadLine();
        }

        public void SaveWorld()
        {
            // TODO: Error checking, make sure valid state, etc.
            mProcess.StandardInput.WriteLine("save");
        }

        public void Exit()
        {
            // TODO: Error checking, make sure valid state, etc.
            mProcess.StandardInput.WriteLine("exit");
        }

        public void WaitForExitAndThenClose()
        {
            mProcess.WaitForExit();
            mProcess.Close();
        }

        private Process CreateServerProcessObject()
        {
            var process = new Process();

            // Make there's nothing particuarly wacky about our inputs.
            if (string.IsNullOrWhiteSpace(ServerExePath))
            {
                throw new InvalidOperationException("Server exe path was not provided");
            }

            if (string.IsNullOrWhiteSpace(WorkingDirectory))
            {
                throw new InvalidOperationException("Working directory was not provided");
            }

            if (string.IsNullOrWhiteSpace(ConfigFileName))
            {
                throw new InvalidOperationException("Config file name was not provided");
            }

            if (!File.Exists(ServerExePath))
            {
                throw new InvalidOperationException("Server exe does not exist");
            }

            if (!Directory.Exists(WorkingDirectory))
            {
                throw new InvalidOperationException("Working directory does not exist");
            }

            if (!File.Exists(Path.Combine(WorkingDirectory, ConfigFileName)))
            {
                throw new InvalidOperationException("Server config file does not exist");
            }
            
            // Set up the server exe path, working directory and make sure the terraria server exe gets passed the
            // path to its configuration file.
            process.StartInfo.FileName = ServerExePath;
            process.StartInfo.WorkingDirectory = WorkingDirectory;
            process.StartInfo.Arguments = string.Format("-config {0}", ConfigFileName);

            // Don't use the shell for execution. Helps with security issues.
            process.StartInfo.UseShellExecute = false;

            // Redirect standard error and output to an event handler that will asynchronously read from the terraria
            // server process.
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.OutputDataReceived += new DataReceivedEventHandler(TerrariaOutputHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(TerrariaErrorHandler);

            // Redirect standard input to a stream we control, which allows the server wrapper to control what commands
            // are sent to the server process.
            process.StartInfo.RedirectStandardInput = true;

            return process;
        }

        private void TerrariaOutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            string outputLine = CleanTerrariaServerOutputLine(outLine.Data);

            if (!string.IsNullOrEmpty(outputLine))
            {
                // Read the server's output to determine current state of server.
                if (outputLine == "Starting server...")
                {
                    CurrentState = ServerState.Starting;
                }
                else if (outputLine == "Type 'help' for a list of commands.")
                {
                    CurrentState = ServerState.Ready;
                }
                else if (outputLine.StartsWith("Saving world data:"))
                {
                    CurrentState = ServerState.Saving;
                }
                else if (outputLine.StartsWith("Backing up world file..."))
                {
                    CurrentState = ServerState.Ready;
                }

                // Write the raw server output to the console.
                Console.WriteLine(outLine.Data);
            }
        }

        private void TerrariaErrorHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                Console.WriteLine("! {0}", outLine.Data);
            }
        }

        private static string CleanTerrariaServerOutputLine(string rawOutputLine)
        {
            // Strip leading colons from the output. Not sure why they get written, or occasionally why a bunch
            // are written out.
            string cleanedInputLine = null;

            if (rawOutputLine != null)
            {
                for (int i = 0; i < rawOutputLine.Length; ++i)
                {
                    if (rawOutputLine[i] != ' ' && rawOutputLine[i] != ':')
                    {
                        cleanedInputLine = rawOutputLine.Substring(i, rawOutputLine.Length - i);
                        break;
                    }
                }
            }

            return cleanedInputLine ?? string.Empty;
        }
    }
}
