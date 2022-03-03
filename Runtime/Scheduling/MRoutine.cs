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

        MonoBehaviour Attachment;
        internal void Attach(MonoBehaviour behaviour)
        {
            Attachment = behaviour;
            Record.Register(Attachment, this);
        }

        CheckDelegate Checker;
        internal void Check(CheckDelegate value)
        {
            Checker = value;
        }
        public delegate bool CheckDelegate();

        Stack<IEnumerator> Numerators;
        IAwaitable Awaitable;

        public event Action OnFinish;

        void Configure(IEnumerator numerator)
        {
            Numerators.Push(numerator);
        }

        bool Evaluate()
        {
            if (Checker is not null)
                if (Checker() is true)
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
                    Numerators.Pop();
                    continue;
                }

                if (iterator.Current is IAwaitable instruction)
                    Awaitable = instruction;
                else if (iterator.Current is IEnumerator nest)
                    Numerators.Push(nest);
                else if (iterator.Current is Handle handle)
                    Awaitable = Wait.Routine(handle);
                else if (Command.Converter.TryProcess(iterator.Current, out Awaitable) == false)
                    Debug.LogWarning($"MRoutine Cannot yield on '{iterator.Current}' of Type '{iterator.Current.GetType()}'");
            }

            return false;
        }

        void Dispose()
        {
            ID += 1;
            Numerators.Clear();

            Attachment = null;
            Checker = null;

            if (OnFinish is not null)
            {
                OnFinish?.Invoke();
                OnFinish = null;
            }

            if (Awaitable is not null)
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

            return End(handle.routine);
        }
        public static bool StopAll(MonoBehaviour behaviour)
        {
            var list = Record.IterateActive(behaviour);

            if (list.Count == 0)
                return false;

            for (int i = 0; i < list.Count; i++)
                End(list[i]);

            return true;
        }

        internal static bool End(MRoutine routine)
        {
            if (Runtime.Processing.Remove(routine) == false)
            {
                Debug.LogWarning($"Trying to End Non-Running MRoutine");
                return false;
            }

            if (routine.Attachment != null)
                Record.Unregister(routine.Attachment, routine);

            routine.Dispose();
            return true;
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

            internal bool Valdiate()
            {
                if (IsValid == false)
                {
                    Debug.LogWarning("Trying to Perform Operation on Invalid Routine Handle");
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Attach this routine to a Monobehaviour so it can be stopped using MRoutine.StopAll(behaviour)
            /// </summary>
            /// <param name="behaviour"></param>
            /// <returns></returns>
            /// <exception cref="ArgumentNullException"></exception>
            /// <exception cref="InvalidOperationException"></exception>
            public Handle Attach(MonoBehaviour behaviour)
            {
                if (behaviour == null)
                    throw new ArgumentNullException(nameof(behaviour));

                if (Valdiate() == false)
                    return this;

                if (routine.Attachment != null)
                    throw new InvalidOperationException($"Routine Already has '{routine.Attachment}' Attached");

                routine.Attach(behaviour);

                return this;
            }

            /// <summary>
            /// Add a constant running checking method, this method will cause the routine to stop if it returns true
            /// </summary>
            /// <param name="method"></param>
            /// <returns></returns>
            public Handle Check(CheckDelegate method)
            {
                if (Valdiate() == false)
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
                if (Valdiate() == false)
                    return this;

                OnFinish += method;
                return this;
            }

            /// <summary>
            /// Starts the current routine, must be called explicitly when creating routines
            /// </summary>
            public void Start()
            {
                Runtime.Initiate(routine);
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
            internal static HashSet<MRoutine> Processing;
            static HashSet<MRoutine> Removals;

            internal static void Initiate(MRoutine routine)
            {
                Processing.Add(routine);

                if (routine.Evaluate())
                    End(routine);
            }

            static void Update()
            {
                foreach (var routine in Processing)
                    if (routine.Evaluate())
                        Removals.Add(routine);

                if(Removals.Count > 0)
                {
                    foreach (var routine in Removals)
                        End(routine);

                    Removals.Clear();
                }
            }
            
            static Runtime()
            {
                Processing = new HashSet<MRoutine>();
                Removals = new HashSet<MRoutine>();

                MUtility.RegisterPlayerLoop<Update>(Update);
            }
        }

        internal static class Record
        {
            internal static Dictionary<MonoBehaviour, HashSet<MRoutine>> Collection { get; }
            static List<MRoutine> cache;

            internal static void Register(MonoBehaviour behaviour, MRoutine routine)
            {
                if (Collection.TryGetValue(behaviour, out var set) == false)
                {
                    DisposablePool.HashSet<MRoutine>.Lease(out set);
                    Collection.Add(behaviour, set);
                }

                set.Add(routine);
            }
            internal static bool Unregister(MonoBehaviour behaviour, MRoutine routine)
            {
                if (Collection.TryGetValue(behaviour, out var set) == false)
                    return false;

                var removed = set.Remove(routine);

                if (set.Count == 0)
                {
                    Collection.Remove(behaviour);
                    DisposablePool.HashSet<MRoutine>.Return(set);
                }

                return removed;
            }

            internal static List<MRoutine> IterateActive(MonoBehaviour behaviour)
            {
                cache.Clear();

                if (Collection.TryGetValue(behaviour, out var set))
                {
                    foreach (var routine in set)
                        cache.Add(routine);
                }

                return cache;
            }

            static Record()
            {
                Collection = new Dictionary<MonoBehaviour, HashSet<MRoutine>>();
                cache = new List<MRoutine>();
            }
        }
        #endregion

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

            public static object Routine(Func<IEnumerator> function, bool attach = true)
            {
                var numerator = function();
                return Routine(numerator, attach);
            }
            public static object Routine(IEnumerator numerator, bool attach = true)
            {
                if (attach) return numerator;

                var handle = Create(numerator);
                return Routine(handle);
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