using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;
using UnityEngine.PlayerLoop;

using Object = UnityEngine.Object;

namespace MB
{
    public class MRoutine
    {
        ulong ID;

        OnDisableCallback Callback;
        void Attach(MonoBehaviour behaviour)
        {
            Callback = OnDisableCallback.Retrieve(behaviour);
            Callback.Event += Stop;
        }

        CheckDelegate Checker;
        void Check(CheckDelegate value)
        {
            Checker = value;
        }
        public delegate bool CheckDelegate();

        Stack<IEnumerator> Numerators;
        IAwaitable Awaitable;

        bool StopSignal;

        public event Action OnFinish;

        void Configure(IEnumerator numerator)
        {
            Numerators.Push(numerator);
        }

        bool Evaluate()
        {
            if (StopSignal)
                return true;

            if (Checker is not null)
                if (Checker() is false)
                    return true;

            if (Awaitable is not null && Awaitable.Evaluate())
            {
                Awaitable.Dispose();
                Awaitable = null;
            }

            while (Awaitable is null)
            {
                if (Numerators.TryPeek(out var iterator) == false)
                    return true;

                if (iterator.MoveNext() == false)
                {
                    if (StopSignal)
                        return true;

                    Numerators.Pop();
                    continue;
                }

                if (TryConvertIteratorToAwaitable(iterator.Current, out Awaitable) == false)
                {
                    if (iterator.Current is IEnumerator nest)
                        Numerators.Push(nest);
                    else
                        Debug.LogWarning($"MRoutine Cannot yield on '{iterator.Current}' of Type '{iterator.Current.GetType()}'");
                } 
            }

            return false;
        }

        void Stop() => Runtime.Stop(this);

        void Dispose()
        {
            ID += 1;
            Numerators.Clear();

            Checker = null;

            StopSignal = false;

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
        }

        public MRoutine()
        {
            ID = 0;
            Numerators = new Stack<IEnumerator>();
        }

        //Static Utility

        #region Lifetime
        public static Handle Create(Func<IEnumerator> method)
        {
            var numerator = method();
            return Create(numerator);
        }
        public static Handle Create(IEnumerator numerator)
        {
            var routine = Pool<MRoutine>.Lease();
            routine.Configure(numerator);

            return new Handle(routine);
        }

        public static bool Stop(Handle handle)
        {
            if (handle.IsValid == false)
            {
                Debug.LogWarning("Trying to Stop MRoutine with Invalid Handle");
                return false;
            }

            Runtime.Stop(handle.routine);
            return true;
        }
        #endregion

        static bool TryConvertIteratorToAwaitable(object target, out IAwaitable awaitable)
        {
            if (target is IAwaitable instruction)
            {
                awaitable = instruction;
                return true;
            }
            else if (Command.Converter.TryProcess(target, out awaitable))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Object used to reference a routine operation
        /// </summary>
        public readonly struct Handle
        {
            internal readonly MRoutine routine;
            internal readonly ulong ID;

            public bool IsAssigned => routine is not null;

            /// <summary>
            /// Returns true as long as the routine that this handle was retrieved for is still running
            /// </summary>
            public bool IsValid
            {
                get
                {
                    if (IsAssigned == false)
                        return false;

                    return routine.ID == ID;
                }
            }

            public event Action OnFinish
            {
                add => routine.OnFinish += value;
                remove => routine.OnFinish -= value;
            }

            internal bool Validate()
            {
                if (IsValid == false)
                {
                    Debug.LogWarning("Trying to Perform Operation on Invalid Routine Handle");
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Attach this routine to a GameObject so it can be stopped when said GameObject is disabled
            /// </summary>
            /// <param name="behaviour"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentNullException"></exception>
            /// <exception cref="InvalidOperationException"></exception>
            public Handle Attach(MonoBehaviour behaviour)
            {
                if (behaviour == null)
                    throw new ArgumentNullException(nameof(behaviour));

                if (Validate() == false)
                    return this;

                if (routine.Callback != null)
                    throw new InvalidOperationException($"Routine Already has been Attached");

                routine.Attach(behaviour);

                return this;
            }

            /// <summary>
            /// Add a constant running checking method, this method will cause the routine to stop if it returns false
            /// </summary>
            /// <param name="method"></param>
            /// <returns></returns>
            public Handle Check(CheckDelegate method)
            {
                if (Validate() == false)
                    return this;

                routine.Check(method);

                return this;
            }

            /// <summary>
            /// Register a callback for when this routine finishes executing
            /// </summary>
            /// <param name="method"></param>
            /// <returns></returns>
            public Handle Callback(Action method)
            {
                if (Validate() == false)
                    return this;

                OnFinish += method;
                return this;
            }

            /// <summary>
            /// Starts the current routine, must be called explicitly when creating routines
            /// </summary>
            public Handle Start()
            {
                Runtime.Start(routine);
                return this;
            }

            /// <summary>
            /// Attempts to stop the referenced Routine
            /// </summary>
            /// <returns>true if stopped successfully</returns>
            public bool Stop() => MRoutine.Stop(this);

            public Handle(MRoutine routine)
            {
                this.routine = routine;
                ID = routine.ID;
            }
        }

        internal static class Runtime
        {
            static HashSet<MRoutine> Processing;
            static List<MRoutine> List;

            internal static void Start(MRoutine routine)
            {
                if (Processing.Add(routine) == false)
                    throw new InvalidOperationException("Routine Already Started");

                if (routine.Evaluate())
                    Dispose(routine);
            }

            internal static void Stop(MRoutine routine)
            {
                routine.StopSignal = true;
            }

            static void Update()
            {
                foreach (var routine in Processing)
                    List.Add(routine);

                for (int i = 0; i < List.Count; i++)
                    if (List[i].Evaluate())
                        Dispose(List[i]);

                List.Clear();
            }

            static bool Dispose(MRoutine routine)
            {
                if (Processing.Remove(routine) == false)
                {
                    Debug.LogWarning($"Trying to End Non-Running MRoutine");
                    return false;
                }

                routine.Dispose();
                return true;
            }

            static Runtime()
            {
                Processing = new HashSet<MRoutine>();
                List = new List<MRoutine>();

                MUtility.RegisterPlayerLoop<Update>(Update);
            }
        }

        #region Commands
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

            public static Command.WaitForAllOperation All(params object[] targets)
            {
                var instance = Command.WaitForAllOperation.Lease();

                var list = instance.list;
                list.EnsureCapacity(targets.Length);

                for (int i = 0; i < targets.Length; i++)
                {
                    if (TryConvertIteratorToAwaitable(targets[i], out var awaitable))
                        list.Add(awaitable);
                    else
                        Debug.LogWarning($"MRoutine Cannot yield on '{targets}' of Type '{targets.GetType()}'");
                }

                return instance;
            }
        }

        /// <summary>
        /// Interface to implement to make an object awaitable by a routine
        /// </summary>
        public interface IAwaitable
        {
            /// <summary>
            /// Invoked every frame to check if the current awaitable has finished
            /// </summary>
            /// <returns>True when the current operation is finished</returns>
            bool Evaluate();

            /// <summary>
            /// Used to clean up awaitables, will be executed when the awaitable is out of scope
            /// </summary>
            void Dispose();
        }

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

                public override bool Evaluate() => handle.IsValid == false;
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
        /// Generic awaitable abstract class to be used for yield returns, will dispose of it's self when done with
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract class Command<T> : Command
            where T : Command<T>, new()
        {
            public T Self { get; }

            public override void Dispose() => Pool<T>.Return(Self);

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
    }
}