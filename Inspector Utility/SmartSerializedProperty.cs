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

using System.Reflection;

namespace MB
{
    public class SmartSerializedProperty<T>
    {
        public SerializedProperty Property;

        public SerializedObject SerializedObject => Property.serializedObject;

        public bool IsExpanded
        {
            get => Property.isExpanded;
            set => Property.isExpanded = value;
        }

        public string Path => Property.propertyPath;

        #region Indexing
        public SerializedProperty this[int index] => Property.GetArrayElementAtIndex(index);
        public SmartSerializedProperty<TTarget> GetAt<TTarget>(int index)
        {
            var target = this[index];

            return new SmartSerializedProperty<TTarget>(target);
        }

        public SerializedProperty this[string name] => Property.FindPropertyRelative(name);
        public SmartSerializedProperty<TTarget> Find<TTarget>(string name)
        {
            var target = this[name];

            return new SmartSerializedProperty<TTarget>(target);
        }
        #endregion

        #region Managed Object
        object Cache_RootManagedObject;
        /// <summary>
        /// The Direct Root Managed Object of this Property
        /// </summary>
        public object RootManagedObject
        {
            get
            {
                ValidateManagedObjectReflection();

                return Cache_RootManagedObject;
            }
            protected set
            {
                Cache_RootManagedObject = value;
            }
        }

        IList Cache_RootManagedCollection;
        /// <summary>
        /// The Direct Root Managed Object of this Property if it is an IList
        /// </summary>
        public IList RootManagedCollection
        {
            get
            {
                ValidateManagedObjectReflection();

                return Cache_RootManagedCollection;
            }
            protected set
            {
                Cache_RootManagedCollection = value;
            }
        }

        bool Cache_IsCollectionElement;
        /// <summary>
        /// Is this Property a Part of a Collection (Array/List)
        /// </summary>
        public bool IsCollectionElement
        {
            get
            {
                ValidateManagedObjectReflection();

                return Cache_IsCollectionElement;
            }
            protected set
            {
                Cache_IsCollectionElement = value;
            }
        }

        int Cache_Index;
        /// <summary>
        /// The Index of this Property in It's Root List, if It's a Part of a Collection
        /// </summary>
        public int Index
        {
            get
            {
                ValidateManagedObjectReflection();

                return Cache_Index;
            }
            protected set
            {
                Cache_Index = value;
            }
        }

        FieldInfo Cache_FieldInfo;
        /// <summary>
        /// The Field info of either this Property or the Collection it Belongs to
        /// </summary>
        public FieldInfo FieldInfo
        {
            get
            {
                ValidateManagedObjectReflection();

                return Cache_FieldInfo;
            }
            protected set
            {
                Cache_FieldInfo = value;
            }
        }

        Type Cache_ManagedType;
        /// <summary>
        /// The Managed Type of this Property
        /// </summary>
        public Type ManagedType
        {
            get
            {
                ValidateManagedObjectReflection();

                return Cache_ManagedType;
            }
            protected set
            {
                Cache_ManagedType = value;
            }
        }

        /// <summary>
        /// Managed Object Property for this Property,
        /// Will get and Set for the first TargetObject of the SerializedObject (No Multi-Editing)
        /// </summary>
        public T ManagedObject
        {
            get
            {
                if (IsCollectionElement)
                    return (T)RootManagedCollection[Index];
                else
                    return (T)FieldInfo.GetValue(RootManagedObject);
            }
            set
            {
                if (Equals(ManagedObject, value)) return;

                Undo.RecordObjects(Property.serializedObject.targetObjects, $"Set {Property.displayName}");

                if (IsCollectionElement)
                    RootManagedCollection[Index] = value;
                else
                    FieldInfo.SetValue(RootManagedObject, value);

                ForceUpdate();

                SetDirty();
            }
        }

        void ValidateManagedObjectReflection()
        {
            if (Cache_RootManagedObject != null) return;

            GetManagedObjectReflectors(Property, out Cache_RootManagedObject, out Cache_IsCollectionElement, out Cache_Index, out Cache_FieldInfo);

            ManagedType = FieldInfo.FieldType;
            if (IsCollectionElement) ManagedType = GetCollectionArgument(ManagedType);

            if (IsCollectionElement) RootManagedCollection = RootManagedObject as IList;
        }
        #endregion

        /// <summary>
        /// Current Value of this Property as Defined by the Generic Constraint,
        /// Uses Property.propertyType to Determine what Value to Return
        /// </summary>
        public T Value
        {
            get => GetValue();
            set => SetValue(value);
        }

        #region Value Getters & Setters
        T GetValue() => (T)GetValueInternal();
        object GetValueInternal()
        {
            switch (Property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return Property.intValue;

                case SerializedPropertyType.Boolean:
                    return Property.boolValue;

                case SerializedPropertyType.Float:
                    return Property.floatValue;

                case SerializedPropertyType.String:
                    return Property.stringValue;

                case SerializedPropertyType.Color:
                    return Property.colorValue;

                case SerializedPropertyType.ObjectReference:
                    return Property.objectReferenceValue;

                case SerializedPropertyType.LayerMask:
                    return Property.intValue;

                case SerializedPropertyType.Vector2:
                    return Property.vector2Value;

                case SerializedPropertyType.Vector3:
                    return Property.vector3Value;

                case SerializedPropertyType.Vector4:
                    return Property.vector4Value;

                case SerializedPropertyType.Rect:
                    return Property.rectValue;

                case SerializedPropertyType.ArraySize:
                    return Property.arraySize;

                case SerializedPropertyType.AnimationCurve:
                    return Property.animationCurveValue;

                case SerializedPropertyType.Bounds:
                    return Property.boundsValue;

                case SerializedPropertyType.Quaternion:
                    return Property.quaternionValue;

                case SerializedPropertyType.ExposedReference:
                    return Property.exposedReferenceValue;

                case SerializedPropertyType.Vector2Int:
                    return Property.vector2IntValue;

                case SerializedPropertyType.Vector3Int:
                    return Property.vector3IntValue;

                case SerializedPropertyType.RectInt:
                    return Property.rectIntValue;

                case SerializedPropertyType.BoundsInt:
                    return Property.boundsIntValue;

                case SerializedPropertyType.Enum:
                    return Enum.ToObject(ManagedType, Property.intValue);

                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Generic:
                    return FieldInfo.GetValue(RootManagedObject);

                //Not Implemented ----------------------------

                case SerializedPropertyType.FixedBufferSize:
                    break;

                case SerializedPropertyType.Gradient:
                    break;

                case SerializedPropertyType.Character:
                    break;
            }

            throw new NotImplementedException($"Smart Serialized Property Cannot Handle Property of Type {Property.propertyType} Yet");
        }

        void SetValue(T value) => SetValueInternal(value);
        void SetValueInternal(object value)
        {
            switch (Property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    Property.intValue = (int)value;
                    return;

                case SerializedPropertyType.Boolean:
                    Property.boolValue = (bool)value;
                    return;

                case SerializedPropertyType.Float:
                    Property.floatValue = (float)value;
                    return;

                case SerializedPropertyType.String:
                    Property.stringValue = (string)value;
                    return;

                case SerializedPropertyType.Color:
                    Property.colorValue = (Color)value;
                    return;

                case SerializedPropertyType.ObjectReference:
                    Property.objectReferenceValue = (Object)value;
                    return;

                case SerializedPropertyType.LayerMask:
                    Property.intValue = (int)value;
                    return;

                case SerializedPropertyType.Vector2:
                    Property.vector2Value = (Vector2)value;
                    return;

                case SerializedPropertyType.Vector3:
                    Property.vector3Value = (Vector3)value;
                    return;

                case SerializedPropertyType.Vector4:
                    Property.vector4Value = (Vector4)value;
                    return;

                case SerializedPropertyType.Rect:
                    Property.rectValue = (Rect)value;
                    return;

                case SerializedPropertyType.ArraySize:
                    Property.arraySize = (int)value;
                    return;

                case SerializedPropertyType.AnimationCurve:
                    Property.animationCurveValue = (AnimationCurve)value;
                    return;

                case SerializedPropertyType.Bounds:
                    Property.boundsValue = (Bounds)value;
                    return;

                case SerializedPropertyType.Quaternion:
                    Property.quaternionValue = (Quaternion)value;
                    return;

                case SerializedPropertyType.ExposedReference:
                    Property.exposedReferenceValue = (Object)value;
                    return;

                case SerializedPropertyType.Vector2Int:
                    Property.vector2IntValue = (Vector2Int)value;
                    return;

                case SerializedPropertyType.Vector3Int:
                    Property.vector3IntValue = (Vector3Int)value;
                    return;

                case SerializedPropertyType.RectInt:
                    Property.rectIntValue = (RectInt)value;
                    return;

                case SerializedPropertyType.BoundsInt:
                    Property.boundsIntValue = (BoundsInt)value;
                    return;

                case SerializedPropertyType.Enum:
                    Property.intValue = ChangeType<int>(value);
                    return;

                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.Generic:
                    Property.managedReferenceValue = value;
                    return;

                //Not Implemented ----------------------------

                case SerializedPropertyType.FixedBufferSize:
                    break;

                case SerializedPropertyType.Gradient:
                    break;

                case SerializedPropertyType.Character:
                    break;
            }

            throw new NotImplementedException($"Smart Serialized Property Cannot Handle Property of {Property.type} Yet");
        }
        #endregion

        public void ForceUpdate() => SerializedObject.Update();

        public void SetDirty()
        {
            foreach (var target in SerializedObject.targetObjects)
                EditorUtility.SetDirty(target);
        }

        public SmartSerializedProperty(SerializedProperty property)
        {
            this.Property = property;
        }

        //Static Utility

        public static implicit operator SerializedProperty(SmartSerializedProperty<T> target) => target.Property;
        public static implicit operator SmartSerializedProperty<T>(SerializedProperty target) => target.MakeSmart<T>();

        public static TTarget ChangeType<TTarget>(object value) => (TTarget)Convert.ChangeType(value, typeof(TTarget));

        public static void GetManagedObjectReflectors(SerializedProperty property, out object root, out bool isCollection, out int index, out FieldInfo field)
        {
            var flags = BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var path = property.propertyPath.Replace(".Array.data", ".");
            var segments = path.Split('.');

            root = property.serializedObject.targetObject;
            field = default;

            isCollection = false;
            index = 0;

            for (int i = 0; i < segments.Length; i++)
            {
                isCollection = segments[i].BeginsWith('[') && segments[i].EndsWith(']');
                index = isCollection ? ParsePathSegmentIndex(segments[i]) : 0;

                var target = root;

                if (isCollection)
                {
                    target = (target as IList)[index];
                }
                else
                {
                    var type = target.GetType();
                    field = type.GetField(segments[i], flags);

                    target = field.GetValue(target);
                }

                if (i + 1 != segments.Length) root = target;
            }
        }

        static int ParsePathSegmentIndex(string text)
        {
            text = text.RemoveAll('[', ']');

            return int.Parse(text);
        }

        static Type GetCollectionArgument(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            return type.GetGenericArguments()[0];
        }
    }

    public static class SmartSerializedPropertyExtensions
    {
        public static SmartSerializedProperty<object> MakeSmart(this SerializedProperty property)
        {
            return new SmartSerializedProperty<object>(property);
        }

        public static SmartSerializedProperty<T> MakeSmart<T>(this SerializedProperty property)
        {
            return new SmartSerializedProperty<T>(property);
        }
    }
}