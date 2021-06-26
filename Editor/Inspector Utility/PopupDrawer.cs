#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

using UnityEditor;
using UnityEditorInternal;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace MB
{
    public class PopupDrawer<TData>
    {
        public GUIContent Label;

        public List<TData> List;

        public GUIContent[] Options;

        public int Index;

        public bool IsNone => Index < 0;

        public TData Selection
        {
            get
            {
                if (IsNone)
                    return default;

                return List[Index];
            }
        }

        public void Populate(IEnumerable<TData> data, Func<TData, string> getText, TData selection)
        {
            List = data.ToList();

            Options = new GUIContent[List.Count + 1];

            Index = List.IndexOf(selection);
            Options[0] = new GUIContent("None");

            for (int i = 0; i < List.Count; i++)
            {
                var text = getText(List[i]);

                Options[i + 1] = new GUIContent(text);
            }
        }

        public float GetHeight() => EditorGUIUtility.singleLineHeight;

        public void Draw(Rect rect)
        {
            EditorGUI.BeginChangeCheck();

            Index = EditorGUI.Popup(rect, Label, Index + 1, Options) - 1;

            if (EditorGUI.EndChangeCheck())
                Select();
        }

        public delegate void SelectDelegate(int index, bool isNone, TData selection);
        public event SelectDelegate OnSelect;
        public void Select()
        {
            OnSelect?.Invoke(Index, IsNone, Selection);
        }

        public PopupDrawer(string label) : this(new GUIContent(label)) { }
        public PopupDrawer(GUIContent label)
        {
            this.Label = label;
        }
    }
}
#endif