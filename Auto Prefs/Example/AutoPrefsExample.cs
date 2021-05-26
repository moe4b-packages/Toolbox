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
using System.Net;

namespace MB
{
	public class AutoPrefsExample : MonoBehaviour
	{
        void Start()
        {
            AutoPrefs.Configure(CustomJsonConveters.Collection);

            try
            {
                AutoPrefs.Load();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception when Loading AutoPrefs, Will Reset Prefs" +
                    $"{Environment.NewLine}" +
                    $"Exception: {ex}");

                AutoPrefs.Reset();
            }

            AutoPrefs.AutoSave.All = false;

            if (AutoPrefs.Contains("First Launch") == false)
            {
                AutoPrefs.Set("First Launch", true);

                AutoPrefs.Set("sample1", 42);
                AutoPrefs.Set("sample2", "Hello World");
                AutoPrefs.Set("sample3", DateTime.Now);
                AutoPrefs.Set("sample4", IPAddress.Loopback);
                AutoPrefs.Set("sample5", new SampleData() { number = 68, text = "Late Gator", date = DateTime.Now });
                AutoPrefs.Set("sample6", Color.yellow);
            }

            AutoPrefs.TryRead("sample1", out int sample1);
            Debug.Log(sample1);

            AutoPrefs.TryRead("sample2", out string sample2);
            Debug.Log(sample2);

            AutoPrefs.TryRead("sample3", out DateTime sample3);
            Debug.Log(sample3);

            AutoPrefs.TryRead("sample4", out IPAddress sample4);
            Debug.Log(sample4);

            AutoPrefs.TryRead("sample5", out SampleData sample5);
            Debug.Log(sample5);

            AutoPrefs.TryRead("sample6", out Color sample6);
            Debug.Log(sample6);

            AutoPrefs.Save();
        }

        [JsonObject]
        [Serializable]
        public class SampleData
        {
            public int number;
            public string text;
            public DateTime date;

            public override string ToString() => $"{number} | {text} | {date}";
        }

        public static class CustomJsonConveters
        {
            public static JsonConverter[] Collection { get; private set; } = new JsonConverter[]
            {
            new IPAddressConverter(),
            new ColorConverter(),
            };

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
}