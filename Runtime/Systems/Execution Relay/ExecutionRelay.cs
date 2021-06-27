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
		public IOperation[] Operations { get; protected set; }

        public virtual void Configure()
        {
            Operations = GetComponentsInChildren<IOperation>(true);
        }

        public virtual void Init() { }

        public virtual void Invoke()
        {
            for (int i = 0; i < Operations.Length; i++)
                Operations[i].Execute();
        }
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