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
    /// <summary>
    /// A system to establish references between different (and usually nested) classes
    /// </summary>
    public static class References
    {
        public static IList<IReference<TContext>> Set<TContext>(TContext context) where TContext : Component => Set(context, context.gameObject);
        public static IList<IReference<TContext>> Set<TContext>(TContext context, GameObject gameObject)
        {
            var targets = gameObject.GetComponentsInChildren<IReference<TContext>>(true);

            Set(context, targets);

            return targets;
        }

        public static void Set<TContext, TReference>(TContext context, Func<IEnumerable<TReference>> function)
            where TReference : IReference<TContext>
        {
            var collection = function();

            Set(context, collection);
        }
        public static void Set<TContext, TReference>(TContext context, IEnumerable<TReference> collection)
            where TReference : IReference<TContext>
        {
            foreach (var item in collection)
                Set(context, item);
        }

        public static void Set<TContext>(TContext context, IReference<TContext> instance) => instance.Set(context);
    }

    public interface IReference<TContext>
    {
        void Set(TContext context);
    }
}