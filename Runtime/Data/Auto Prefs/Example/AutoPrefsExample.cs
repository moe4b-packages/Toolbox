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
            AutoPreferences.AutoSave.All = false;

            if (AutoPreferences.Contains("First Launch") == false)
            {
                AutoPreferences.Set("First Launch", true);

                AutoPreferences.Set("sample1", 42);
                AutoPreferences.Set("sample2", "Hello World");
                AutoPreferences.Set("sample3", DateTime.Now);
                AutoPreferences.Set("sample4", IPAddress.Loopback);
                AutoPreferences.Set("sample5", new SampleData() { number = 68, text = "Late Gator", date = DateTime.Now });
                AutoPreferences.Set("sample6", Color.yellow);
            }

            var sample1 = AutoPreferences.Read<int>("sample1");
            Debug.Log(sample1);

            var sample2 = AutoPreferences.Read<string>("sample2");
            Debug.Log(sample2);

            var sample3 = AutoPreferences.Read<DateTime>("sample3");
            Debug.Log(sample3);

            var sample4 = AutoPreferences.Read<IPAddress>("sample4");
            Debug.Log(sample4);

            var sample5 = AutoPreferences.Read<SampleData>("sample5");
            Debug.Log(sample5);

            var sample6 = AutoPreferences.Read<Color>("sample6");
            Debug.Log(sample6);

            AutoPreferences.Context.Save();
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