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
    /// <summary>
    /// an attribute to be used on fields with an enum flag to allow setting multiple values for that field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class EnumFlagAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(EnumFlagAttribute))]
        public class Drawer : PersistantPropertyDrawer
        {
            Type type;

            protected override void Init()
            {
                base.Init();

                type = MUtility.SerializedPropertyType.Retrieve(Property);
            }

            public override float CalculateHeight()
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void Draw(Rect rect)
            {
                var value = IntToEnum(Property.intValue, type);

                value = EditorGUI.EnumFlagsField(rect, Label, value);

                Property.longValue = EnumToInt(value);
            }

            //Static Utility

            public static Enum IntToEnum(int value, Type type) => (Enum)Enum.ToObject(type, value);

            static int EnumToInt(Enum value) => (int)Convert.ChangeType(value, typeof(int));
        }
#endif
    }
}