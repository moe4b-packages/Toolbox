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

namespace MB
{
    public class AutoKeyCollection<TKey>
    {
        protected HashSet<TKey> reserve;

        protected Queue<TKey> vacant;

        public TKey Index { get; set; }

        public delegate TKey IncrementDelegate(TKey value);
        public IncrementDelegate Incrementor { get; protected set; }

        public TKey Reserve()
        {
            if (vacant.Count > 0)
            {
                var key = vacant.Dequeue();

                reserve.Add(key);

                return key;
            }
            else
            {
                var key = Increment();

                reserve.Add(key);

                return key;
            }
        }

        TKey Increment()
        {
            var key = Index;

            Index = Incrementor(Index);

            return key;
        }

        public bool Contains(TKey key) => reserve.Contains(key);

        public bool Free(TKey key)
        {
            if (Contains(key) == false) return false;

            reserve.Remove(key);
            vacant.Enqueue(key);
            return true;
        }
        public void FreeAll()
        {
            var items = reserve.ToArray();

            foreach (var entry in items)
                Free(entry);
        }

        public void Clear()
        {
            Index = default;
            reserve.Clear();
            vacant.Clear();
        }

        public AutoKeyCollection(IncrementDelegate incrementor)
        {
            reserve = new HashSet<TKey>();
            vacant = new Queue<TKey>();

            Index = default;
            this.Incrementor = incrementor;
        }
    }
}