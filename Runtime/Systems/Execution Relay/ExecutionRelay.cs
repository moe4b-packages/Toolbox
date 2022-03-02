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
    /// A relay used to execute operations based on invokation,
    /// used mostly with UI in the form of a ButtonExecutionRelay that will execute operations on button click
    /// </summary>
	public abstract class ExecutionRelay : MonoBehaviour, IInitialize
    {
        public static class Paths
        {
            public const string Root = Toolbox.Paths.Box + "Execution Relay/";

            public const string Variants = Root + "Variants/";

            public const string Utility = Root + "Utility/";
        }

        [SerializeField]
        Operation operation;

        protected virtual void Reset()
        {
            operation = GetComponentInChildren<Operation>();
        }

        public virtual void Configure() { }
        public virtual void Initialize() { }

        public virtual Coroutine Invoke() => operation.Execute();
    }

	public abstract class ExecutionRelay<TContext> : ExecutionRelay
        where TContext : class
    {
        public TContext Context { get; protected set; }

        protected virtual TContext GetContext() => GetComponent<TContext>();

        protected abstract void RegisterContext();

        public override void Configure()
        {
            base.Configure();

            Context = GetContext();

            RegisterContext();
        }
    }
}