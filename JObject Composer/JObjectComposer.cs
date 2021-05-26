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
        void Retrieve(string[] path, out JToken token, out string id, bool create = false)
        {
            id = path.Last();

            token = Retrieve(path, cut: 1, create: create);
        }

        JToken Retrieve(string[] path, int cut = 0, bool create = false)
        {
            JToken token = Context;

            for (int i = 0; i < path.Length - cut; i++)
            {
                var target = token[path[i]];

                if (target == null)
                {
                    if (create == false)
                        return null;

                    target = new JObject();
                    (token as JObject).Add(path[i], target);
                }

                token = target;
            }

            return token;
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

        #region Read
        public virtual T Read<T>(string id, T fallback = default) => Read(Context, id, fallback: fallback);

        public virtual T Read<T>(string[] path, string id, T fallback = default)
        {
            var token = Retrieve(path);

            return Read(token, id, fallback: fallback);
        }

        public virtual T Read<T>(string[] path, T fallback = default)
        {
            Retrieve(path, out var token, out var id);

            return Read(token, id, fallback: fallback);
        }

        public virtual T Read<T>(JToken token, string id, T fallback = default)
        {
            TryRead(token, id, out T value, fallback: fallback);

            return value;
        }
        #endregion

        #region Try Read
        public virtual bool TryRead<T>(string id, out T value, T fallback = default)
        {
            return TryRead(Context, id, out value, fallback: fallback);
        }

        public virtual bool TryRead<T>(string[] path, string id, out T value, T fallback = default)
        {
            var token = Retrieve(path);

            return TryRead(token, id, out value, fallback: fallback);
        }

        public virtual bool TryRead<T>(string[] path, out T value, T fallback = default)
        {
            Retrieve(path, out var token, out var id);

            return TryRead(token, id, out value, fallback: fallback);
        }

        public virtual bool TryRead<T>(JToken token, string id, out T value, T fallback = default)
        {
            ValidateState();

            var target = token?[id];

            if (target == null)
            {
                value = fallback;
                return false;
            }

            try
            {
                value = target.ToObject<T>(Serializer);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot Read {id} of {token.Path} as {typeof(T).Name}" +
                    $"{Environment.NewLine}" +
                    $"Exception: {ex}", ex);
            }

            return true;
        }
        #endregion

        #region Contains
        public virtual bool Contains(string id) => Contains(Context, id);

        public virtual bool Contains(string[] path, string id)
        {
            var token = Retrieve(path);

            return Contains(token, id);
        }

        public virtual bool Contains(string[] path)
        {
            Retrieve(path, out var token, out var id);

            return Contains(token, id);
        }

        public virtual bool Contains(JToken token, string id)
        {
            ValidateState();

            var target = token[id];

            if (target == null)
                return false;

            return true;
        }
        #endregion

        #region Set
        public virtual void Set<T>(string id, T value) => Set(Context, id, value);

        public virtual void Set<T>(string[] path, string id, T value)
        {
            var token = Retrieve(path, create: true);

            Set(token, id, value);
        }

        public virtual void Set<T>(string[] path, T value)
        {
            Retrieve(path, out var token, out var id, create: true);

            Set(token, id, value);
        }

        public virtual void Set<T>(JToken token, string id, T value)
        {
            ValidateState();

            token[id] = JToken.FromObject(value, Serializer);

            InvokeChange();
        }
        #endregion

        #region Remove
        public virtual bool Remove(string id) => Remove(Context, id);

        public virtual bool Remove(string[] path, string id)
        {
            var token = Retrieve(path);

            return Remove(token, id);
        }

        public virtual bool Remove(string[] path)
        {
            Retrieve(path, out var token, out var id);

            return Remove(token, id);
        }

        public virtual bool Remove(JToken token, string id)
        {
            ValidateState();

            var target = token as JObject;

            if (target.Remove(id) == false)
                return false;

            InvokeChange();
            return true;
        }
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