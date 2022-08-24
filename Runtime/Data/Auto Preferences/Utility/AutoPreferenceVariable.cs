using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

namespace MB
{
    /// <summary>
    /// Type that can be used as a wrapper for an auto preference variable,
    /// loaded on construction and updated after modifications
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AutoPreferenceVariable<T>
    {
        AutoPreferences AutoPreferences => AutoPreferences.Instance;

        public string ID { get; }

        T backing;
        public T Value
        {
            get => backing;
            set
            {
                backing = value;

                Save();
            }
        }

        public void Load()
        {
            backing = AutoPreferences.Read(ID, backing);
        }
        public void Save()
        {
            AutoPreferences.Set(ID, backing);
        }

        public override string ToString() => backing.ToString();

        public AutoPreferenceVariable(string ID) : this(ID, default) { }
        public AutoPreferenceVariable(string ID, T initial)
        {
            this.ID = ID;
            this.backing = initial;

            Load();
        }

        public static implicit operator T(AutoPreferenceVariable<T> variable) => variable.Value;
    }
}