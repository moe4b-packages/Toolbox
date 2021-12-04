#if UNITY_EDITOR
///Custom reimplementation of and idea provided here:
///https://github.com/arimger/Unity-Editor-Toolbox
///Which in it's own self is a custom reimplementation of an idea originally provided here:
///https://github.com/roboryantron/UnityEditorJunkie/blob/master/Assets/SearchableEnum/Code/Editor/SearchablePopup.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using UnityEditor;
using UnityEditor.IMGUI.Controls;

using UnityEngine;

namespace MB.ThirdParty
{
    public abstract class SearchablePopup : PopupWindowContent
    {
        public static class Style
        {
            public static readonly float Indent = 8.0f;
            public static readonly float Height = EditorGUIUtility.singleLineHeight;
            public static readonly float Padding = 6.0f;
            public static readonly float Spacing = EditorGUIUtility.standardVerticalSpacing;

            public static GUIStyle Toolbar;
            public static GUIStyle Scrollbar;
            public static GUIStyle Selection;
            public static GUIStyle SearchBox;
            public static GUIStyle ShowCancelButton;
            public static GUIStyle HideCancelButton;
            public static GUIStyle RichLabel;

            static Style()
            {
                Toolbar = new GUIStyle(EditorStyles.toolbar);
                Scrollbar = new GUIStyle(GUI.skin.verticalScrollbar);
                Selection = new GUIStyle("SelectionRect");
                SearchBox = new GUIStyle("ToolbarSeachTextField");
                ShowCancelButton = new GUIStyle("ToolbarSeachCancelButton");
                HideCancelButton = new GUIStyle("ToolbarSeachCancelButtonEmpty");

                RichLabel = new GUIStyle(GUI.skin.label);
                RichLabel.richText = true;
            }
        }

        public static void Show(Rect rect, IList<string> source, int index, Action<int> callback, bool includeNone = false)
        {
            var indexes = new int[source.Count];
            for (int i = 0; i < indexes.Length; i++)
                indexes[i] = i;

            SearchablePopup<int>.Show(rect, indexes, index, Parser, callback, includeNone: includeNone);

            GUIContent Parser(int index) => new GUIContent(source[index], source[index]);
        }
    }

    /// <summary>
    /// Searchable popup content that allows user to filter items using a provided string value.
    /// </summary>
    public class SearchablePopup<T> : SearchablePopup
    {
        readonly Rect rect;

        readonly SearchField SearchBar;

        readonly IList<T> Source;

        readonly ParserDelegate Parser;
        public delegate GUIContent ParserDelegate(T item);

        readonly List<PopupItem> Items;

        bool IncludeNone;
        string LastFilter;

        public bool Filter() => Filter("");
        public bool Filter(string term)
        {
            if (LastFilter == term)
                return false;

            Items.Clear();

            LastFilter = term;
            term = term.ToLower();

            if (IncludeNone)
            {
                var content = new GUIContent("None");
                var item = new PopupItem(default, content);

                Items.Add(item);
            }

            for (int i = 0; i < Source.Count; i++)
            {
                var content = Parser(Source[i]);

                if (Compare(term, content.text))
                {
                    var item = new PopupItem(Source[i], content);

                    if (string.Equals(term, content.text, StringComparison.CurrentCultureIgnoreCase))
                        Items.Insert(0, item);
                    else
                        Items.Add(item);
                }
            }

            return true;

            bool Compare(string filter, string entry)
            {
                if (string.IsNullOrEmpty(filter))
                    return true;

                if (entry.ToLower().Contains(filter))
                    return true;

                return false;
            }
        }

        public readonly struct PopupItem
        {
            public readonly T Source { get; }
            public GUIContent Content { get; }

            public PopupItem(T source, GUIContent content)
            {
                this.Source = source;
                this.Content = content;
            }
        }

        readonly Action<T> Callback;

        int OptionIndex = -1;
        int ScrollIndex = -1;

        Vector2 Scroll;

        public override void OnOpen()
        {
            EditorApplication.update += editorWindow.Repaint;
        }
        
        public override void OnGUI(Rect rect)
        {
            HandleInput();

            float yMax;

            //Draw Toolbar
            {
                //set toolbar rect using the built-in toolbar height
                var area = new Rect(0, 0, rect.width, Style.Toolbar.fixedHeight);

                yMax = area.yMax;

                DrawToolbar(area);
            }

            //Draw Content
            {
                //set content rect adjusted to the toolbar container
                var area = Rect.MinMaxRect(0, yMax, rect.xMax, rect.yMax);

                DrawContent(area);
            }
            
            //additionally disable all GUI controls
            GUI.enabled = false;
        }

        void HandleInput()
        {
            var currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            if (currentEvent.keyCode == KeyCode.DownArrow)
            {
                GUI.FocusControl(null);
                OptionIndex = Mathf.Min(Items.Count - 1, OptionIndex + 1);
                ScrollIndex = OptionIndex;
                currentEvent.Use();
            }

            if (currentEvent.keyCode == KeyCode.UpArrow)
            {
                GUI.FocusControl(null);

                OptionIndex = Mathf.Max(0, OptionIndex - 1);
                ScrollIndex = OptionIndex;

                currentEvent.Use();
            }

            if (currentEvent.keyCode == KeyCode.Return)
            {
                GUI.FocusControl(null);

                if (OptionIndex >= 0 && OptionIndex < Items.Count)
                    SelectItem(Items[OptionIndex].Source);

                currentEvent.Use();
            }

            if (currentEvent.keyCode == KeyCode.Escape)
            {
                GUI.FocusControl(null);
                editorWindow.Close();
            }
        }

        void DrawToolbar(Rect rect)
        {
            var uEvent = Event.current;

            if (uEvent.type == EventType.Repaint)
                Style.Toolbar.Draw(rect, false, false, false, false);

            rect.xMin += Style.Padding;
            rect.xMax -= Style.Padding;
            rect.yMin += Style.Spacing;
            rect.yMax -= Style.Spacing;

            //draw toolbar and try to search for valid text
            var term = SearchBar.OnGUI(rect, LastFilter, Style.SearchBox,
                Style.ShowCancelButton, Style.HideCancelButton);

            Filter(term);
        }
        void DrawContent(Rect rect)
        {
            var uEvent = Event.current;

            //Prepare base rect for the whole content window
            var contentRect = new Rect(0, 0, rect.width - Style.Scrollbar.fixedWidth, Items.Count * Style.Height);
            var elementRect = new Rect(0, 0, rect.width, Style.Height);

            Scroll = GUI.BeginScrollView(rect, Scroll, contentRect);

            for (var i = 0; i < Items.Count; i++)
            {
                if (uEvent.type == EventType.Repaint && ScrollIndex == i)
                {
                    GUI.ScrollTo(elementRect);
                    ScrollIndex = -1;
                }

                if (elementRect.Contains(uEvent.mousePosition))
                {
                    if (uEvent.type == EventType.MouseMove || uEvent.type == EventType.ScrollWheel)
                        OptionIndex = i;

                    if (uEvent.type == EventType.MouseDown)
                        SelectItem(Items[i].Source);
                }

                if (OptionIndex == i)
                    GUI.Box(elementRect, GUIContent.none, Style.Selection);

                //Draw Label
                {
                    elementRect.xMin += Style.Indent;

                    GUI.Label(elementRect, Items[i].Content, Style.RichLabel);

                    elementRect.xMin -= Style.Indent;
                    elementRect.y = elementRect.yMax;
                }
            }

            GUI.EndScrollView();
        }

        void SelectItem(T source)
        {
            Callback(source);
        }

        public override void OnClose()
        {
            EditorApplication.update -= editorWindow.Repaint;
        }

        internal SearchablePopup(Rect rect, IList<T> source, T current, ParserDelegate parser, Action<T> callback, bool includeNone)
        {
            this.rect = rect;
            this.Source = source;
            this.Parser = parser;
            this.IncludeNone = includeNone;

            Items = new List<PopupItem>();
            Filter();

            SearchBar = new SearchField();
            SearchBar.SetFocus();

            OptionIndex = ScrollIndex = source.IndexOf(current);

            if(includeNone)
            {
                OptionIndex += 1;
                ScrollIndex += 1;
            }

            this.Callback = callback;
            this.Callback += x => editorWindow.Close();
        }

        public override Vector2 GetWindowSize() => new Vector2(rect.width, 400f);

        //Static Utlity

        public static void Show(Rect rect, IList<T> source, T current, ParserDelegate parser, Action<T> callback, bool includeNone = false)
        {
            var window = new SearchablePopup<T>(rect, source, current, parser, callback, includeNone);

            PopupWindow.Show(rect, window);
        }
    }
}
#endif