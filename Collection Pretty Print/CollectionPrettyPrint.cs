using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace MB
{
    /// <summary>
    /// Provides a user readable string from a collection,
    /// mostly usefull for debugging when you are too lazy to set break points :)
    /// </summary>
    public static class CollectionPrettyPrint
    {
        public static string ToCollectionString<T>(this IEnumerable<T> collection, Func<T, string> ToString = null)
        {
            if (collection == null) return "null";

            if (ToString == null) ToString = DefaultToString;

            var text = "";

            text += "[ ";
            text += collection.Select(x => ToString(x)).Aggregate((x, y) => $"{x}, {y}");
            text += " ]";

            return text;
        }

        public static string DefaultToString<T>(T item)
        {
            if (item == null) return "null";

            if (IsKeyValue(item))
                return KeyValueToString(item);
            else
                return item.ToString();
        }

        public static bool IsKeyValue(object instance)
        {
            var type = instance.GetType();

            if (type.IsGenericType == false) return false;

            return type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
        }
        public static string KeyValueToString(object pair)
        {
            var key = pair.GetType().GetProperty("Key", BindingFlags.Public | BindingFlags.Instance);
            var value = pair.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);

            return key.GetValue(pair) + ": " + value.GetValue(pair);
        }

        public static bool IsValidCollection(object instance)
        {
            if (instance is string) return false;

            return instance is IEnumerable;
        }
    }
}