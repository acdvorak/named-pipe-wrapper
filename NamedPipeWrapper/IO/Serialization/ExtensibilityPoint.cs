namespace NamedPipeWrapper.IO.Serialization
{
	public static class ExtensibilityPoint
	{
		public delegate ISerializer CreateSerializerDelegate();

		public static CreateSerializerDelegate CreateSerializer = () => new BinaryFormatterSerializer();
	}
}
