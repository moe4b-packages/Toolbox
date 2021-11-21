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
using MB.ThirdParty;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using System.Text;
using System.Reflection;

namespace MB
{
	/// <summary>
	/// Provides a Unity serializable monoscript backed type field (can whistand name changes & moving),
	/// can be restricted to use specific derived types using the nested SelectionAttribute
	/// </summary>
	[Serializable]
	public class SerializedMonoScript : XSerializedType
	{
		[SerializeField]
		Object asset = default;
		public Object Asset => asset;

        public override void OnBeforeSerialize()
        {
			base.OnBeforeSerialize();

#if UNITY_EDITOR
			if (asset == null)
			{
				id = string.Empty;
				return;
			}

			var type = (asset as MonoScript).GetClass();

			id = TypeToID(type);
#endif
		}

		public static implicit operator Type(SerializedMonoScript target) => target.Type;

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(SerializedMonoScript), true)]
		[CustomPropertyDrawer(typeof(SelectionAttribute), true)]
		class Drawer : BaseDrawer<MonoScript>
		{
			public override Type IgnoreAttributeType => typeof(IgnoreAttribute);

			public override Type HandlerToType(MonoScript handler)
            {
				if (handler == null)
					return null;

				return handler.GetClass();
			}

            public override List<MonoScript> Query(Type argument)
            {
				var list = new List<MonoScript>();

				for (int i = 0; i < AssetCollection.List.Count; i++)
				{
					if (AssetCollection.List[i] is MonoScript script)
					{
						var type = script.GetClass();

						if (argument.IsAssignableFrom(type))
							list.Add(script);
					}
				}

				return list;
			}

            public override MonoScript RetrieveHandler(SerializedProperty property, Type selection)
            {
				var asset = property.FindPropertyRelative("asset");

				return asset.objectReferenceValue as MonoScript;
			}
        }
#endif

		[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
		public class SelectionAttribute : BaseXSerializedTypeSelectionAttribute
		{
			public SelectionAttribute(Type argument) : base(argument) { }
			public SelectionAttribute(Type argument, SerializedTypeInclude includes) : base(argument, includes) { }
		}

		[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
		public class IgnoreAttribute : BaseXSerializedTypeIgnoreAttribute { }
	}
}