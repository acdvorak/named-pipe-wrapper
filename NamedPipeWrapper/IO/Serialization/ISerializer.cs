namespace NamedPipeWrapper.IO.Serialization
{
	public interface ISerializer
	{
		T Deserialize<T>(byte[] data) where T : class;

		byte[] Serialize<T>(T value) where T : class;
	}
}