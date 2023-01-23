using System;

namespace NamedPipeWrapper.Json
{
    /// <summary>
    /// JsonExtension
    /// </summary>
    public static class JsonExtension
    {
        /// <summary>
        /// JsonDeserialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T JsonDeserialize<T>(this string value)
        {
            return JsonUtils.DeserializeObject<T>(value);
        }

        /// <summary>
        /// JsonSerialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string JsonSerialize<T>(this T value)
        {
            return JsonUtils.SerializeObject<T>(value);
        }

        /// <summary>
        /// Is Json Format class?
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsTypeJson(Type type)
        {
            if (type.IsArray)
            {
                return false;
            }
            return type.IsClass;
        }
        
    }
}