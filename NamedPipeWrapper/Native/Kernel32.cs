using System.Runtime.InteropServices;

namespace NamedPipeWrapper.Native
{
    internal sealed class Kernel32
    {
        /// <summary>
        /// Waits until either a time-out interval elapses or an instance of the specified named pipe is available for connection
        /// </summary>
        /// <param name="lpNamedPipeName">The name of the named pipe. The string must include the name of the computer on which the server process is executing.</param>
        /// <param name="nTimeOut">If no instances of the specified named pipe exist, the WaitNamedPipe function returns immediately, regardless of the time-out value.</param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool WaitNamedPipe(string lpNamedPipeName, uint nTimeOut);
    }
}