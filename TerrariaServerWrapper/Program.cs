using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scott.Terraria.Server;

namespace TerrariaServerWrapper
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///  TODO: Try to capture exceptions.
    /// </remarks>
    class Program
    {
        private const string TerrariaServerExePath = @"E:\TerrariaServer\TerrariaServer.exe";
        private const string TerrariaWorkingDirectory = @"E:\TerrariaServer";
        private const string TerrariaServerConfigName = @"serverconfig.txt";
        private const int ReadLineTimeoutMillisecs = 1000;

        public static void Main(string[] args)
        {
            var server = new TerrariaServerProcess();

            server.ConfigFileName = TerrariaServerConfigName;
            server.WorkingDirectory = TerrariaWorkingDirectory;
            server.ServerExePath = TerrariaServerExePath;

            try
            {
                server.Start();

                // Wait for the process to exit.
                Console.WriteLine("Waiting for exit...");

                // Console input (this is temporary proof of concept).
                while (server.IsRunning)
                {
                    // Wait for the server's state to be ready.
                    //  TODO: Put a timeout in this, so if it isn't ready after X seconds display a message on the
                    //        console that we're still waiting for the server to not be busy.
                    while (server.CurrentState != ServerState.Ready && server.IsRunning)
                    {
                        continue;
                    }

                    // Get the console user's next command. Set a low timeout (around a second) so we can notify the
                    // user if the server's state has changed.
                    string inputText = null;
                    Console.Write("==>");

                    do
                    {
                        inputText = ConsoleInput.TryReadLine(ReadLineTimeoutMillisecs);
                    } while (inputText == null && server.IsRunning);
                    
                    // Execute the command that was typed in.
                    if (inputText == "save")
                    {
                        server.SaveWorld();
                    }
                    else if (inputText == "exit")
                    {
                        server.Exit();
                    }
                }

                // Server is exiting. Wait until it has finished.
                server.WaitForExitAndThenClose();
                Console.WriteLine("Done!");
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
        }
    }
}
