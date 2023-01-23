using Newtonsoft.Json;

namespace NamedPipeWrapper.Json
{
    /// <summary>
    /// JsonUtils
    /// </summary>
    public static class JsonUtils
    {
        /// <summary>
        /// Settings
        /// </summary>
        public static JsonSerializerSettings Settings { get; set; } = new JsonSerializerSettings()
        {
            MaxDepth = 64,
            NullValueHandling = NullValueHandling.Ignore,
        };

        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string Serialize<T>(T value)
        {
            return SerializeObject(value);
        }

        /// <summary>
        /// SerializeObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string SerializeObject<T>(T value)
        {
            return JsonConvert.SerializeObject(value, Settings);
        }
        /// <summary>
        /// Deserialize
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Deserialize<T>(string value)
        {
            return DeserializeObject<T>(value);
        }
        /// <summary>
        /// DeserializeObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T DeserializeObject<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, Settings);
        }
    }
}