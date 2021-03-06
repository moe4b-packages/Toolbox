#if UNITY_EDITOR
#define DEBUG
#endif

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
        public Type Owner { get; }

        public JObject Context { get; protected set; }
        public bool IsLoaded => Context != null;

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

        public void Configure(JsonSerializerSettings settings)
        {
            Serializer = JsonSerializer.Create(settings);
        }

        public void Load(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                Context = new JObject();
            }
            else
            {
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
        }

        public void Clear() => Load(string.Empty);

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

#if DEBUG
        void ValidateState()
        {

            if (IsLoaded == false)
                throw FormatException("Accessed When not Loaded");

            Exception FormatException(string text) => new Exception($"JObject Composer Used in '{Owner.FullName}' {text}");
        }
#endif
        #endregion

        #region Controls

        #region Read
        public virtual T Read<T>(string path, T fallback = default)
        {
#if DEBUG
            ValidateState();
#endif

            Retrieve(path, out var token, out var id);

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
#if DEBUG
            ValidateState();
#endif

            Retrieve(path, out var token, out var id);

            return Contains(token, id);
        }

        protected virtual bool Contains(JToken token, string id)
        {
            var target = token?[id];

            if (target == null)
                return false;

            return true;
        }
        #endregion

        #region Set
        public virtual void Set(string path, object value)
        {
#if DEBUG
            ValidateState();
#endif

            Retrieve(path, out var token, out var id, create: true);

            Set(token, id, value);
        }

        protected virtual void Set(JToken token, string id, object value)
        {
            token[id] = JToken.FromObject(value, Serializer);

            InvokeChange();
        }
        #endregion

        #region Remove
        public virtual bool Remove(string path)
        {
#if DEBUG
            ValidateState();
#endif

            Retrieve(path, out var token, out var id);

            return Remove(token, id);
        }

        protected virtual bool Remove(JToken token, string id)
        {
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
#if DEBUG
            ValidateState();
#endif

            return Context.ToString(Formatting.Indented);
        }

        public JObjectComposer(Type owner)
        {
            this.Owner = owner;
        }

        public static JObjectComposer Create<TOwner>()
        {
            var owner = typeof(TOwner);
            return new JObjectComposer(owner);
        }
    }
}