using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Vertx.Extensions;
using Object = UnityEngine.Object;

namespace Vertx.Editor
{
	internal static class AssetListUtility
	{
		[OnOpenAsset]
		static bool OpenAsset(int instanceID, int line)
		{
			Object @object = EditorUtility.InstanceIDToObject(instanceID);
			if (!(@object is AssetListConfiguration configAsset)) return false;

			AssetListWindow assetListWindow = AssetListWindow.GetUnopenedWindow();
			assetListWindow.InitialiseWithConfiguration(configAsset);
			assetListWindow.Show();
			return true;
		}

		public static List<Object> LoadAssetsByTypeName(string assemblyQualifiedName, out Type type, out bool isComponent)
		{
			type = Type.GetType(assemblyQualifiedName);
			isComponent = false;

			List<Object> values = new List<Object>();
			if (type == null)
				return values;

			isComponent = type.IsSubclassOf(typeof(Component));
			if (isComponent)
			{
				GameObject[] prefabs = EditorUtils.LoadAssetsOfType<GameObject>();
				values = new List<Object>();
				foreach (GameObject prefab in prefabs)
					values.AddRange(prefab.GetComponents(type));
				return values;
			}

			string[] guids = AssetDatabase.FindAssets($"t:{type.FullName}");
			if (guids.Length == 0)
			{
				guids = AssetDatabase.FindAssets($"t:{type.Name}");
				if (guids.Length == 0)
					return values;
			}

			foreach (string guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset != null)
					values.Add(asset);
			}

			return values;
		}

		public static object GetSortableValue(SerializedProperty property)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
					return property.intValue;
				case SerializedPropertyType.Boolean:
					return property.boolValue;
				case SerializedPropertyType.Float:
					return property.floatValue;
				case SerializedPropertyType.String:
					return property.stringValue;
				case SerializedPropertyType.ObjectReference:
					return property.objectReferenceValue.name;
				case SerializedPropertyType.LayerMask:
					return property.intValue;
				case SerializedPropertyType.Enum:
					return property.intValue;
				case SerializedPropertyType.ArraySize:
					return property.arraySize;
				case SerializedPropertyType.Character:
					return property.stringValue;
				//Values below this point are sorted completely arbitrarily
				case SerializedPropertyType.Color:
					return property.colorValue.grayscale;
				case SerializedPropertyType.Vector2:
					return property.vector2Value.sqrMagnitude;
				case SerializedPropertyType.Vector3:
					return property.vector3Value.sqrMagnitude;
				case SerializedPropertyType.Vector4:
					return property.vector4Value.sqrMagnitude;
				case SerializedPropertyType.AnimationCurve:
					return property.animationCurveValue.length;
				case SerializedPropertyType.Quaternion:
					return property.quaternionValue.eulerAngles.y;
				case SerializedPropertyType.FixedBufferSize:
					return property.fixedBufferSize;
				case SerializedPropertyType.Vector2Int:
					return property.vector2IntValue.sqrMagnitude;
				case SerializedPropertyType.Vector3Int:
					return property.vector3IntValue.sqrMagnitude;
				case SerializedPropertyType.Rect:
					return property.rectValue.size.sqrMagnitude;
				case SerializedPropertyType.RectInt:
					return property.rectIntValue.size.sqrMagnitude;
				case SerializedPropertyType.Bounds:
					return property.boundsValue.size.sqrMagnitude;
				case SerializedPropertyType.BoundsInt:
					return property.boundsIntValue.size.sqrMagnitude;
				case SerializedPropertyType.ExposedReference:
					return property.exposedReferenceValue.name;
				case SerializedPropertyType.ManagedReference:
				case SerializedPropertyType.Gradient:
				case SerializedPropertyType.Generic:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}