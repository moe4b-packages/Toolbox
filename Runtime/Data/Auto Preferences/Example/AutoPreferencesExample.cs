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
    [AddComponentMenu(Toolbox.Paths.Example + "Auto Preferences Example")]
	public class AutoPreferencesExample : MonoBehaviour
	{
        static AutoPreferences AutoPreferences => AutoPreferences.Instance;

        AutoPreferenceVariable<int> AutoVariable;

        void Start()
        {
            AutoVariable = new AutoPreferenceVariable<int>("Example/Auto Variable");

            AutoVariable.Value += 1;

            if (AutoPreferences.Contains("Example/First Launch") == false)
            {
                AutoPreferences.Set("Example/First Launch", true);

                AutoPreferences.Set("Example/Sample 1", 42);
                AutoPreferences.Set("Example/Sample 2", "Hello World");
                AutoPreferences.Set("Example/Sample 3", DateTime.Now);
                AutoPreferences.Set("Example/Sample 4", IPAddress.Loopback);
                AutoPreferences.Set("Example/Sample 5", new SampleData() { Number = 68, Text = "Late Gator", Date = DateTime.Now });
                AutoPreferences.Set("Example/Sample 6", Color.yellow);
            }

            Debug.Log(AutoVariable);

            var sample1 = AutoPreferences.Read<int>("Example/Sample 1");
            Debug.Log(sample1);

            var sample2 = AutoPreferences.Read<string>("Example/Sample 2");
            Debug.Log(sample2);

            var sample3 = AutoPreferences.Read<DateTime>("Example/Sample 3");
            Debug.Log(sample3);

            var sample4 = AutoPreferences.Read<IPAddress>("Example/Sample 4");
            Debug.Log(sample4);

            var sample5 = AutoPreferences.Read<SampleData>("Example/Sample 5");
            Debug.Log(sample5);

            var sample6 = AutoPreferences.Read<Color>("Example/Sample 6");
            Debug.Log(sample6);

            AutoPreferences.Save();
        }

        [JsonObject]
        [Serializable]
        public class SampleData
        {
            public int Number;
            public string Text;
            public DateTime Date;

            public override string ToString() => $"{Number} | {Text} | {Date}";
        }
    }
}