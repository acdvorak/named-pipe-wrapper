using NamedPipeWrapper;

namespace NamedPipeWrapper
{
    /// <summary>
    /// NamedPipeUtils
    /// </summary>
    public static class NamedPipeUtils
    {
        /// <summary>
        /// PipeFileExists
        /// <code>https://github.com/dotnet/runtime/issues/69604</code>
        /// </summary>
        /// <param name="pipeName"></param>
        /// <returns></returns>
        public static bool PipeFileExists(string pipeName)
        {
            return System.IO.Directory.GetFiles("\\\\.\\pipe\\", pipeName).Length == 1;
        }
    }
}