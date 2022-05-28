using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;
using System;

namespace MB
{
    public struct OneOf<T1, T2>
    {
        public T1 Value1 { get; }
        public bool IsValue1 => Selection == 1;

        public T2 Value2 { get; }
        public bool IsValue2 => Selection == 2;

        public int Selection { get; }
        public bool Assigned => Selection > 0;

        public void Dispatch(Action<T1> callback1, Action<T2> callback2)
        {
            switch (Selection)
            {
                case 1:
                    callback1(Value1);
                    return;

                case 2:
                    callback2(Value2);
                    return;
            }

            throw new InvalidOperationException($"OneOf Value not Assigned");
        }
        public TResult Match<TResult>(Func<T1, TResult> callback1, Func<T2, TResult> callback2)
        {
            switch (Selection)
            {
                case 1:
                    return callback1(Value1);

                case 2:
                    return callback2(Value2);
            }

            throw new InvalidOperationException($"OneOf Value not Assigned");
        }

        private OneOf(T1 value1, T2 value2, int selection)
        {
            this.Value1 = value1;
            this.Value2 = value2;

            this.Selection = selection;
        }
        public OneOf(T1 value1) : this(value1, default, 1) { }
        public OneOf(T2 value2) : this(default, value2, 2) { }

        public static implicit operator OneOf<T1, T2>(T1 value) => new OneOf<T1, T2>(value);
        public static implicit operator OneOf<T1, T2>(T2 value) => new OneOf<T1, T2>(value);
    }

    public struct OneOf<T1, T2, T3>
    {
        public T1 Value1 { get; }
        public bool IsValue1 => Selection == 1;

        public T2 Value2 { get; }
        public bool IsValue2 => Selection == 2;

        public T3 Value3 { get; }
        public bool IsValue3 => Selection == 3;

        public int Selection { get; }
        public bool Assigned => Selection > 0;

        public void Dispatch(Action<T1> callback1, Action<T2> callback2, Action<T3> callback3)
        {
            switch (Selection)
            {
                case 1:
                    callback1(Value1);
                    return;

                case 2:
                    callback2(Value2);
                    return;

                case 3:
                    callback3(Value3);
                    return;
            }

            throw new InvalidOperationException($"OneOf Value not Assigned");
        }
        public TResult Match<TResult>(Func<T1, TResult> callback1, Func<T2, TResult> callback2, Func<T3, TResult> callback3)
        {
            switch (Selection)
            {
                case 1:
                    return callback1(Value1);

                case 2:
                    return callback2(Value2);

                case 3:
                    return callback3(Value3);
            }

            throw new InvalidOperationException($"OneOf Value not Assigned");
        }

        private OneOf(T1 value1, T2 value2, T3 value3, int selection)
        {
            this.Value1 = value1;
            this.Value2 = value2;
            this.Value3 = value3;

            this.Selection = selection;
        }
        public OneOf(T1 value1) : this(value1, default, default, 1) { }
        public OneOf(T2 value2) : this(default, value2, default, 2) { }
        public OneOf(T3 value3) : this(default, default, value3, 3) { }

        public static implicit operator OneOf<T1, T2, T3>(T1 value) => new OneOf<T1, T2, T3>(value);
        public static implicit operator OneOf<T1, T2, T3>(T2 value) => new OneOf<T1, T2, T3>(value);
        public static implicit operator OneOf<T1, T2, T3>(T3 value) => new OneOf<T1, T2, T3>(value);
    }
}