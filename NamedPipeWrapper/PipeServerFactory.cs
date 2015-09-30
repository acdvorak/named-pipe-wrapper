using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace NamedPipeWrapper
{
	static class PipeServerFactory
	{
		public static NamedPipeServerStream CreateAndConnectPipe(string pipeName)
		{
			var pipe = CreatePipe(pipeName);
			pipe.WaitForConnection();

			return pipe;
		}

		public static NamedPipeServerStream CreatePipe(string pipeName)
		{
			return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
		}

		public static NamedPipeServerStream CreateAndConnectPipe(string pipeName, int bufferSize, PipeSecurity security)
		{
			var pipe = CreatePipe(pipeName, bufferSize, security);
			pipe.WaitForConnection();

			return pipe;
		}

		public static NamedPipeServerStream CreatePipe(string pipeName, int bufferSize, PipeSecurity security)
		{
			return new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.WriteThrough, bufferSize, bufferSize, security);
		}
	}
}
