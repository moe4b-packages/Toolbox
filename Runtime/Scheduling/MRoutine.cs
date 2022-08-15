using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;

using Object = UnityEngine.Object;

namespace MB
{
    public static class MRoutine
    {
        #region Lifetime
        public static Handle Create(Func<IEnumerator> method)
        {
            var numerator = method();
            return Create(numerator);
        }
        public static Handle Create(IEnumerator numerator)
        {
            var routine = Pool<Processor>.Lease();
            return new Handle(routine, numerator);
        }

        public static bool Stop(Handle handle) => handle.Stop();
        #endregion

        /// <summary>
        /// Object used to reference a routine operation
        /// </summary>
        public readonly struct Handle
        {
            internal readonly Processor processor;
            internal readonly IEnumerator numerator;
            internal readonly ulong ID;

            /// <summary>
            /// Flag used to indiciate if this handle is valid, handles are invalid once their routines finish
            /// </summary>
            public bool IsValid
            {
                get
                {
                    if (processor == null)
                        return false;

                    return processor.ID == ID;
                }
            }

            /// <summary>
            /// Is this routine not finished?
            /// </summary>
            public bool IsProcessing => IsValid;

            /// <summary>
            /// Has this routine finished?
            /// </summary>
            public bool IsFinished => IsValid == false;

            /// <summary>
            /// Current state of this handle and the routine it's referencing
            /// </summary>
            public RuntimeState State
            {
                get
                {
                    if (processor == null)
                        return RuntimeState.Unassigned;

                    if (processor.ID == ID)
                    {
                        switch (processor.State)
                        {
                            case Processor.RuntimeState.Idle:
                                return RuntimeState.Idle;

                            case Processor.RuntimeState.Running:
                                return RuntimeState.Running;

                            case Processor.RuntimeState.Stopping:
                                return RuntimeState.Stopping;

                            default:
                                throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        return RuntimeState.Complete;
                    }
                }
            }
            public enum RuntimeState
            {
                Unassigned, Idle, Running, Stopping, Complete
            }

            public event Action OnFinish
            {
                add
                {
                    Validate("OnFinish Subscribe");

                    processor.OnFinish += value;
                }
                remove
                {
                    Validate("OnFinish Unsubscribe");

                    processor.OnFinish -= value;
                }
            }

            void Validate(string name)
            {
                if (IsValid == false)
                    throw new InvalidOperationException($"Cannot Perform '{name}' Operation on Invalid Routine Handle");
            }

            #region Controls
            /// <summary>
            /// Attach this routine to a GameObject so it can be stopped when said GameObject is disabled/destroyed,
            /// can only be called once
            /// </summary>
            /// <param name="behaviour"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentNullException"></exception>
            public Handle Attach(GameObject gameObject)
            {
                if (gameObject == null)
                    throw new ArgumentNullException(nameof(gameObject));

                Validate(nameof(Attach));

                processor.Attach(gameObject);

                return this;
            }

            /// <summary>
            /// Add a constant running checking method, this method will cause the routine to stop if it returns false,
            /// can only be called once
            /// </summary>
            /// <param name="method"></param>
            /// <returns></returns>
            public Handle Check(CheckDelegate method)
            {
                Validate(nameof(Check));

                processor.Check(method);

                return this;
            }

            /// <summary>
            /// Register a callback for when this routine finishes executing,
            /// can be called multiple times
            /// </summary>
            /// <param name="method"></param>
            /// <returns></returns>
            public Handle Callback(Action method)
            {
                Validate(nameof(Callback));

                OnFinish += method;
                return this;
            }

            /// <summary>
            /// Starts the current routine, must be called explicitly when creating routines,
            /// must be called at least once
            /// </summary>
            public Handle Start()
            {
                Validate(nameof(Start));

                processor.Start(numerator);
                return this;
            }

            /// <summary>
            /// Attempts to stop the referenced Routine
            /// </summary>
            /// <returns>true if stopped successfully</returns>
            public bool Stop()
            {
                if (IsValid == false)
                    return false;

                return processor.TryStop();
            }
            #endregion

            public TaskAwaiter GetAwaiter()
            {
                return Procedure(this).GetAwaiter();
                async Task Procedure(Handle handle)
                {
                    while (handle.IsProcessing)
                        await Task.Delay(10);
                }
            }

            internal Handle(Processor processor, IEnumerator numerator)
            {
                this.processor = processor;
                this.numerator = numerator;

                ID = processor.ID;
            }
        }

        internal class Processor
        {
            public ulong ID { get; private set; }

            MonobehaviourCallback.GameObject.Disable Callback;
            internal void Attach(GameObject gameObject)
            {
                if (Callback != null)
                    throw new InvalidOperationException($"Routine Already has been Attached");

                Callback = MonobehaviourCallback.GameObject.Disable.Retrieve(gameObject);
                Callback.Event += Stop;
            }

            CheckDelegate Checker;
            internal void Check(CheckDelegate value)
            {
                Checker = value;
            }
            
            Stack<IEnumerator> Numerators;
            IAwaitable Awaitable;

            internal RuntimeState State { get; private set; }
            internal enum RuntimeState
            {
                Idle, Running, Stopping
            }

            public event Action OnFinish;

            internal void Start(IEnumerator numerator)
            {
                if (State != RuntimeState.Idle)
                    throw new InvalidOperationException("Routine Already Started");

                State = RuntimeState.Running;

                Numerators.Push(numerator);

                Runtime.OnProcess += Process;

                Process();
            }

            internal void Stop() => TryStop();
            internal bool TryStop()
            {
                if (State != RuntimeState.Running)
                    return false;

                State = RuntimeState.Stopping;
                return true;
            }

            void Process()
            {
                if (Evaluate())
                    Dispose();
            }
            bool Evaluate()
            {
                if (State == RuntimeState.Stopping)
                    return true;

                if (Checker != null && Checker() == false)
                    return true;

                if (Awaitable != null && Awaitable.Evaluate())
                {
                    Awaitable.Dispose();
                    Awaitable = null;
                }

                while (Awaitable == null)
                {
                    if (Numerators.TryPeek(out var iterator) == false)
                        return true;

                    if (iterator.MoveNext())
                    {
                        if (TryConvertTargetToAwaitable(iterator.Current, out Awaitable) == false)
                        {
                            if (iterator.Current is IEnumerator nest)
                                Numerators.Push(nest);
                            else
                                throw new InvalidOperationException($"MRoutine Cannot yield on '{iterator.Current}' of Type '{iterator.Current.GetType()}'");
                        }
                    }
                    else
                    {
                        if (State == RuntimeState.Stopping)
                            return true;

                        Numerators.Pop();
                    }
                }

                return false;
            }

            void Dispose()
            {
                ID += 1;
                Numerators.Clear();

                Checker = null;

                if (Callback != null)
                {
                    Callback.Event -= Stop;
                    Callback = null;
                }

                if (OnFinish != null)
                {
                    OnFinish?.Invoke();
                    OnFinish = null;
                }

                if (Awaitable != null)
                {
                    Awaitable.Dispose();
                    Awaitable = null;
                }

                State = RuntimeState.Idle;

                Runtime.OnProcess -= Process;

                Pool<Processor>.Return(this);
            }

            public Processor()
            {
                ID = 0;
                Numerators = new Stack<IEnumerator>();
                State = RuntimeState.Idle;
            }
        }

        public delegate bool CheckDelegate();

        internal static class Runtime
        {
            internal static event Action OnProcess;
            static void Process()
            {
                OnProcess?.Invoke();
            }

            static Runtime()
            {
                MUtility.RegisterPlayerLoop<Update>(Process);
            }
        }

        /// <summary>
        /// Class that contains all wait methods
        /// </summary>
        public static class Wait
        {
            public static Command.WaitForSeconds Seconds(float duration) => Seconds(duration, false);
            public static Command.WaitForSeconds Seconds(float duration, bool realtime)
            {
                var instance = Command.WaitForSeconds.Lease();
                instance.duration = duration;
                instance.realtime = realtime;
                return instance;
            }

            public static Command.YieldFrame Frame() => Frame(1);
            public static Command.YieldFrame Frame(int count)
            {
                var instance = Command.YieldFrame.Lease();
                instance.target = Time.frameCount + count;
                return instance;
            }

            public static Command.EvaluteDelegate Until(Func<bool> condition)
            {
                var instance = Command.EvaluteDelegate.Lease();

                instance.condition = condition;
                instance.target = true;

                return instance;
            }
            public static Command.EvaluteDelegate While(Func<bool> condition)
            {
                var instance = Command.EvaluteDelegate.Lease();
                instance.condition = condition;
                instance.target = false;
                return instance;
            }

            public static Command.WaitForRoutine Routine(Handle handle)
            {
                var instance = Command.WaitForRoutine.Lease();
                instance.handle = handle;
                return instance;
            }

            public static Command.WaitForAsyncOperation AsyncOperation(AsyncOperation operation)
            {
                var instance = Command.WaitForAsyncOperation.Lease();
                instance.operation = operation;
                return instance;
            }

            public static Command.WaitForTask Task(Task task)
            {
                var instance = Command.WaitForTask.Lease();
                instance.task = task;
                return instance;
            }

            #region All
            public static Command.WaitForAllOperation All(object target1, object target2)
            {
                var instance = Command.WaitForAllOperation.Lease();

                var list = instance.list;
                list.EnsureCapacity(2);

                TryAddAll(ref list, target1);
                TryAddAll(ref list, target2);

                return instance;
            }
            public static Command.WaitForAllOperation All(object target1, object target2, object target3)
            {
                var instance = Command.WaitForAllOperation.Lease();

                var list = instance.list;
                list.EnsureCapacity(3);

                TryAddAll(ref list, target1);
                TryAddAll(ref list, target2);
                TryAddAll(ref list, target3);

                return instance;
            }
            public static Command.WaitForAllOperation All(object target1, object target2, object target3, object target4)
            {
                var instance = Command.WaitForAllOperation.Lease();

                var list = instance.list;
                list.EnsureCapacity(4);

                TryAddAll(ref list, target1);
                TryAddAll(ref list, target2);
                TryAddAll(ref list, target3);
                TryAddAll(ref list, target4);

                return instance;
            }

            public static Command.WaitForAllOperation All(params object[] targets)
            {
                var instance = Command.WaitForAllOperation.Lease();

                var list = instance.list;
                list.EnsureCapacity(targets.Length);

                for (int i = 0; i < targets.Length; i++)
                {
                    if (TryConvertTargetToAwaitable(targets[i], out var awaitable))
                        list.Add(awaitable);
                    else
                        Debug.LogWarning($"MRoutine Cannot yield on '{targets}' of Type '{targets.GetType()}'");
                }

                return instance;
            }

            static void TryAddAll(ref List<IAwaitable> list, object target)
            {
                if (TryConvertTargetToAwaitable(target, out var awaitable))
                    list.Add(awaitable);
                else
                    Debug.LogWarning($"MRoutine Cannot yield on '{target}' of Type '{target.GetType()}'");
            }
            #endregion
        }

        /// <summary>
        /// Interface to implement to make an object awaitable by a routine
        /// </summary>
        public interface IAwaitable
        {
            /// <summary>
            /// Invoked every frame to check if the current awaitable has finished,
            /// can still be invoked multiple times after returning true
            /// </summary>
            /// <returns>True when the current operation is finished</returns>
            bool Evaluate();

            /// <summary>
            /// Used to clean up awaitables, will be executed when the awaitable is no longer needed
            /// </summary>
            void Dispose();
        }

        #region Commands
        /// <summary>
        /// Awaitable abstract class to be used for yield returns
        /// </summary>
        public abstract class Command : IAwaitable
        {
            public abstract bool Evaluate();

            public abstract void Dispose();

            public Command()
            {

            }

            public static class Converter
            {
                public static Dictionary<Type, ProcessDelegate> Dictionary { get; }

                public delegate IAwaitable ProcessDelegate(object item);
                public delegate IAwaitable ParserDelegate<TItem>(TItem item);

                internal static bool TryProcess(object item, out IAwaitable command)
                {
                    if (item == null)
                    {
                        command = default;
                        return false;
                    }

                    var type = item.GetType();

                    if (Dictionary.TryGetValue(type, out var processor))
                    {
                        command = processor(item);
                        return true;
                    }

                    command = default;
                    return false;
                }

                public static void Register(Type type, ProcessDelegate processor)
                {
                    Dictionary[type] = processor;
                }
                public static void Register<TItem>(ParserDelegate<TItem> processor)
                {
                    var type = typeof(TItem);

                    IAwaitable Surrogate(object item)
                    {
                        var instance = (TItem)item;

                        return processor(instance);
                    }

                    Register(type, Surrogate);
                }

                static Converter()
                {
                    Dictionary = new();

                    Register<Handle>(Wait.Routine);
                    Register<AsyncOperation>(Wait.AsyncOperation);
                    Register<UnityWebRequestAsyncOperation>(Wait.AsyncOperation);
                }
            }

            public class WaitForSeconds : Command<WaitForSeconds>
            {
                public float duration;
                public bool realtime;

                public override bool Evaluate()
                {
                    duration -= realtime ? Time.unscaledDeltaTime : Time.deltaTime;

                    return duration <= 0f;
                }
            }
            public class YieldFrame : Command<YieldFrame>
            {
                public int target;

                public override bool Evaluate() => Time.frameCount >= target;
            }
            public class EvaluteDelegate : Command<EvaluteDelegate>
            {
                public Func<bool> condition;
                public bool target;

                public override bool Evaluate() => condition() == target;

                public override void Dispose()
                {
                    base.Dispose();

                    condition = null;
                }
            }
            public class WaitForRoutine : Command<WaitForRoutine>
            {
                public Handle handle;

                public override bool Evaluate() => handle.IsFinished;
            }
            public class WaitForAsyncOperation : Command<WaitForAsyncOperation>
            {
                public AsyncOperation operation;

                public override bool Evaluate() => operation.isDone;

                public override void Dispose()
                {
                    base.Dispose();

                    operation = null;
                }
            }
            public class WaitForTask : Command<WaitForTask>
            {
                public Task task;

                public override bool Evaluate() => task.IsCompleted;

                public override void Dispose()
                {
                    base.Dispose();

                    task = null;
                }
            }
            public class WaitForAllOperation : Command<WaitForAllOperation>
            {
                public List<IAwaitable> list;

                public override bool Evaluate()
                {
                    bool result = true;

                    for (int i = 0; i < list.Count; i++)
                        if (list[i].Evaluate() == false)
                            result = false;

                    return result;
                }

                public override void Dispose()
                {
                    base.Dispose();

                    for (int i = 0; i < list.Count; i++)
                        list[i].Dispose();

                    list.Clear();
                }

                public WaitForAllOperation()
                {
                    list = new List<IAwaitable>();
                }
            }
        }

        /// <summary>
        /// Generic awaitable poolable abstract class to be used for yield returns, will dispose of it's self when done with
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract class Command<T> : Command
            where T : Command<T>, new()
        {
            public T Self { get; }

            public override void Dispose() => Return(Self);

            public Command()
            {
                Self = this as T;
            }

            public static T Lease() => Pool<T>.Lease();
            public static void Return(T instance) => Pool<T>.Return(instance);
        }
        #endregion

        /// <summary>
        /// Generic pool used to manage everything related to routines
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public static class Pool<T>
                where T : class, new()
        {
            static Stack<T> stack;

            public static T Lease()
            {
                return stack.Count == 0 ? Create() : stack.Pop();
            }

            static T Create()
            {
                return new T();
            }

            public static void Return(T instance)
            {
                stack.Push(instance);
            }

            static Pool()
            {
                stack = new Stack<T>();
            }
        }

        static bool TryConvertTargetToAwaitable(object target, out IAwaitable awaitable)
        {
            if (target is IAwaitable instruction)
            {
                awaitable = instruction;
                return true;
            }
            else if (target is Task task)
            {
                awaitable = Wait.Task(task);
                return true;
            }
            else if (Command.Converter.TryProcess(target, out awaitable))
            {
                return true;
            }

            return false;
        }
    }
}