using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Net;

namespace MB
{
    public static partial class CustomJsonConveters
    {
        public static JsonConverter[] Collection { get; private set; } = new JsonConverter[]
        {
            Instances.IPAddress,
            Instances.Color,
        };

        public static partial class Instances
        {
            public static IPAddressConverter IPAddress { get; } = new IPAddressConverter();

            public static ColorConverter Color { get; } = new ColorConverter();

            public static StringEnumConverter StrinEnum { get; } = new StringEnumConverter();
        }

        public class IPAddressConverter : JsonConverter
        {
            public override bool CanConvert(Type type) => typeof(IPAddress).IsAssignableFrom(type);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var text = value.ToString();

                writer.WriteValue(text);
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var text = (string)reader.Value;

                if (text == null) return null;

                if (text == "localhost") return IPAddress.Loopback;

                return IPAddress.Parse(text);
            }
        }

        public class ColorConverter : JsonConverter
        {
            public override bool CanConvert(Type type) => typeof(Color).IsAssignableFrom(type);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var instance = (Color)value;

                var hex = "#" + ColorUtility.ToHtmlStringRGBA(instance);

                writer.WriteValue(hex);
            }
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var hex = (string)reader.Value;

                if (ColorUtility.TryParseHtmlString(hex, out var color) == false)
                    throw new Exception($"Invalid Hex Color '{hex}' Read");

                return color;
            }
        }
    }
}