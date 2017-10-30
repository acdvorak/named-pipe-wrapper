using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NamedPipeWrapper.IO.Serialization
{
	internal class BinaryFormatterSerializer : ISerializer
	{
		private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();

		public T Deserialize<T>(byte[] data) where T : class
		{
			using (var memoryStream = new MemoryStream(data))
			{
				return (T)_binaryFormatter.Deserialize(memoryStream);
			}
		}

		public byte[] Serialize<T>(T value) where T : class
		{
			using (var memoryStream = new MemoryStream())
			{
				_binaryFormatter.Serialize(memoryStream, value);
				return memoryStream.ToArray();
			}
		}
	}
}