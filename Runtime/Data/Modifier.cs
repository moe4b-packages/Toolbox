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
    public static class Modifier
    {
        public class Constraint : Base<bool>
        {
            public override bool Value
            {
                get
                {
                    for (int i = 0; i < List.Count; i++)
                        if (List[i].Invoke())
                            return true;

                    return false;
                }
            }
        }

        public class Average : Base<float>
        {
            public override float Value
            {
                get
                {
                    var result = 0f;

                    for (int i = 0; i < List.Count; i++)
                        result += List[i].Invoke();

                    if (List.Count == 0) return result;

                    return result / List.Count;
                }
            }
        }

        public class Additive : Base<float>
        {
            public override float Value
            {
                get
                {
                    var result = 0f;

                    for (int i = 0; i < List.Count; i++)
                        result += List[i].Invoke();

                    return result;
                }
            }
        }

        public class Scale : Base<float>
        {
            public override float Value
            {
                get
                {
                    var value = 1f;

                    for (int i = 0; i < List.Count; i++)
                        value *= List[i].Invoke();

                    return value;
                }
            }
        }

        public abstract class Base<T>
        {
            public abstract T Value { get; }

            public List<Delegate> List { get; protected set; }
            public delegate T Delegate();

            public T Retrieve() => Value;

            public void Add(Delegate item) => List.Add(item);
            public void Remove(Delegate item) => List.Remove(item);

            public void Clear() => List.Clear();

            public Base()
            {
                List = new List<Delegate>();
            }
        }
    }
}