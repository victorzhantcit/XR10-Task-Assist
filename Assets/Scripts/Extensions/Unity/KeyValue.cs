using System.Collections.Generic;
using System.Reflection;

namespace Unity.Extensions
{
    public class KeyValue
    {
        public string Key { get; set; }
        public string Value { get; set; }

        public KeyValue()
        {
            Key = string.Empty;
            Value = string.Empty;
        }

        public KeyValue(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }

    public static class KeyValueConverter
    {
        public static List<KeyValue> ToKeyValues<T>(T obj)
        {
            var list = new List<KeyValue>();
            if (obj == null) return list;

            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var value = prop.GetValue(obj)?.ToString() ?? string.Empty;
                list.Add(new KeyValue(prop.Name, value));
            }

            return list;
        }
    }
}
