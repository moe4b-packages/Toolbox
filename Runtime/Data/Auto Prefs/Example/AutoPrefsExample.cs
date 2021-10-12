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
    [AddComponentMenu(Toolbox.Paths.Example + "Auto Prefs Example")]
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

            var sample1 = AutoPrefs.Read<int>("sample1");
            Debug.Log(sample1);

            var sample2 = AutoPrefs.Read<string>("sample2");
            Debug.Log(sample2);

            var sample3 = AutoPrefs.Read<DateTime>("sample3");
            Debug.Log(sample3);

            var sample4 = AutoPrefs.Read<IPAddress>("sample4");
            Debug.Log(sample4);

            var sample5 = AutoPrefs.Read<SampleData>("sample5");
            Debug.Log(sample5);

            var sample6 = AutoPrefs.Read<Color>("sample6");
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
    }
}