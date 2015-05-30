using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaServerWrapper
{
    /// <summary>
    ///  A console input reader that can time out rather than block indefinitely.
    /// </summary>
    /// <remarks>
    ///  This implementation is taken from an answer on Stack Overflow. See the link for the original source:
    ///   http://stackoverflow.com/a/18342182
    /// </remarks>
    public static class ConsoleInput
    {
        private static Thread mInputThread;
        private static AutoResetEvent mGetInput;
        private static AutoResetEvent mGotInput;
        private static string mInput;

        static ConsoleInput()
        {
            mGetInput = new AutoResetEvent(false);
            mGotInput = new AutoResetEvent(false);
            mInputThread = new Thread(ConsoleReadLineImplementation);
            mInputThread.IsBackground = true;
            mInputThread.Start();
        }

        private static void ConsoleReadLineImplementation()
        {
            while (true)
            {
                mGetInput.WaitOne();
                mInput = Console.ReadLine();
                mGotInput.Set();
            }
        }

        public static string TryReadLine(int timeOutMillisecs)
        {
            mGetInput.Set();
            bool success = mGotInput.WaitOne(timeOutMillisecs);

            if (success)
            {
                return mInput;
            }
            else
            {
                return null;
            }
        }
    }
}
