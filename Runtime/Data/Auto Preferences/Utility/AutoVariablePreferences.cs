using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MB
{
    /// <summary>
    /// Component that allows saving any variable to Auto Prefs
    /// </summary>
    [SerializedVariable.Ignore]
    [AddComponentMenu(AutoPreferences.Path + "Auto Variable Preferences")]
    public class AutoVariablePreferences : MonoBehaviour
    {
        [SerializeField]
        [ReadOnly(ReadOnlyPlayMode.PlayMode)]
        Entry[] entries;
        public Entry[] Entries => entries;
        [Serializable]
        public class Entry
        {
            [SerializeField]
            string _ID;
            public string ID
            {
                get => _ID;
                private set => _ID = value;
            }

            [SerializeField]
            [SerializedVariable.Selection(Local = true)]
            SerializedVariable selection;
            public SerializedVariable Selection => selection;

            [SerializeField]
            InitializationOrder order = InitializationOrder.LateStart;
            public InitializationOrder Order => order;

            public VariableInfo Variable { get; private set; }

            public Type Type => Variable.ValueType;

            public object Read() => Variable.Read(selection.Target);
            public void Set(object value) => Variable.Set(selection.Target, value);

            public object LatestValue { get; private set; }

            internal void Initialize()
            {
                if (selection.IsAssigned == false)
                {
                    Debug.LogWarning($"Variable '{ID}' Not Assigned");
                    return;
                }

                Variable = selection.Load();

                LatestValue = Read();

                Load();
            }

            void Load()
            {
                if (AutoPreferences.Contains(ID))
                {
                    LatestValue = AutoPreferences.Read(Type, ID, fallback: LatestValue);
                    Set(LatestValue);
                }
            }
            void Save()
            {
                var value = Read();

                if (Equals(value, LatestValue) == false)
                {
                    AutoPreferences.Set(ID, value);
                }
            }

            internal void Dispose()
            {
                Save();
            }

            public Entry()
            {
                order = InitializationOrder.LateStart;
            }
        }

        public enum InitializationOrder
        {
            LateStart,
            Start,
            Awake,
        }

        void Awake()
        {
            ManualLateStart.Register(LateStart);

            Initialize(InitializationOrder.Awake);
        }

        void Start()
        {
            Initialize(InitializationOrder.Start);
        }

        void LateStart()
        {
            Initialize(InitializationOrder.LateStart);
        }

        void Initialize(InitializationOrder order)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Order != order) continue;

                entries[i].Initialize();
            }
        }

        void OnDestroy()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i].Dispose();
            }
        }
    }
}