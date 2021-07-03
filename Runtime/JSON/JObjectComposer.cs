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

using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace MB
{
    public class JObjectComposer
    {
        public JObject Context { get; protected set; }

        public JsonSerializer Serializer { get; protected set; }

        public static class Path
        {
            public const char Seperator = '/';

            public static string[] PartOut(string text)
            {
                return text.Split(Seperator);
            }

            public static string Compose(string x, string y)
            {
                if (string.IsNullOrEmpty(x)) return y;

                return $"{x}{Seperator}{y}";
            }
            public static string Compose(params string[] list) => list.Join(Seperator);
        }

        #region Configure
        public bool IsConfigured { get; protected set; }

        public void Configure(JsonSerializerSettings settings)
        {
            if (IsConfigured)
                throw new NotImplementedException();

            IsConfigured = true;

            Serializer = JsonSerializer.Create(settings);
        }
        #endregion

        #region Load
        public bool IsLoaded { get; protected set; }

        public void Load(string json)
        {
            IsLoaded = true;

            if (json == null || json == string.Empty)
            {
                Context = new JObject();
                return;
            }

            try
            {
                Context = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot Parse {nameof(JObjectComposer)} from provided JSON" +
                    $"{Environment.NewLine}" +
                    $"Exception: {ex}", ex);
            }
        }
        #endregion

        #region Utility
        public bool Retrieve(string path, out JToken token, out string id, bool create = false)
        {
            var parts = Path.PartOut(path);

            id = parts.Last();

            JToken indexer = token = Context;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                token = indexer[parts[i]];

                if (token == null)
                {
                    if (create == false)
                    {
                        token = null;
                        return false;
                    }

                    token = new JObject();
                    (indexer as JObject).Add(parts[i], token);
                }

                indexer = token;
            }

            return true;
        }

        void ValidateState()
        {
#if UNITY_EDITOR || DEBUG
            if (IsConfigured == false)
                throw FormatException("Not Configured");

            if (IsLoaded == false)
                throw FormatException("Not Loaded");

            Exception FormatException(string text) => new Exception($"{nameof(JObjectComposer)}: {text}");
#endif
        }
        #endregion

        public void Clear()
        {
            ValidateState();

            Context = new JObject();
        }

        #region Controls

        #region Read
        public virtual T Read<T>(string path, T fallback = default)
        {
            Retrieve(path, out var token, out var id);

            ValidateState();

            var target = token?[id];

            if (target == null) return fallback;

            try
            {
                return target.ToObject<T>(Serializer);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot Read {id} of {token.Path} as {typeof(T).Name}" +
                    $"{Environment.NewLine}" +
                    $"Exception: {ex}", ex);
            }
        }

        public virtual object Read(string path, Type data, object fallback = default)
        {
            Retrieve(path, out var token, out var id);

            ValidateState();

            var target = token?[id];

            if (target == null) return fallback;

            try
            {
                return target.ToObject(data, Serializer);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot Read {id} of {token.Path} as {data.Name}" +
                    $"{Environment.NewLine}" +
                    $"Exception: {ex}", ex);
            }
        }
        #endregion

        #region Contains
        public virtual bool Contains(string path)
        {
            Retrieve(path, out var token, out var id);

            return Contains(token, id);
        }

        protected virtual bool Contains(JToken token, string id)
        {
            ValidateState();

            var target = token?[id];

            if (target == null)
                return false;

            return true;
        }
        #endregion

        #region Set
        public virtual void Set(string path, object value)
        {
            Retrieve(path, out var token, out var id, create: true);

            Set(token, id, value);
        }

        protected virtual void Set(JToken token, string id, object value)
        {
            ValidateState();

            token[id] = JToken.FromObject(value, Serializer);

            InvokeChange();
        }
        #endregion

        #region Remove
        public virtual bool Remove(string path)
        {
            Retrieve(path, out var token, out var id);

            return Remove(token, id);
        }

        protected virtual bool Remove(JToken token, string id)
        {
            ValidateState();

            var target = token as JObject;

            if (target.Remove(id) == false)
                return false;

            InvokeChange();
            return true;
        }
        #endregion

        #endregion

        public event Action OnChange;
        void InvokeChange()
        {
            OnChange?.Invoke();
        }

        public string Read()
        {
            ValidateState();

            return Context.ToString(Formatting.Indented);
        }

        public JObjectComposer()
        {

        }
    }
}