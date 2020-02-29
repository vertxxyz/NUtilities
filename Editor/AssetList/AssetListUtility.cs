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

		private static readonly string[] filterArray = {"Assets"};

		public static Object LoadAssetByTypeName(
			Type type,
			bool isComponent,
			AssetType assetType)
		{
			if (type == null)
				return null;

			if (isComponent)
			{
				bool assets = false, scenes = false;
				switch (assetType)
				{
					case AssetType.InAssets:
						assets = true;
						break;
					case AssetType.InScene:
						scenes = true;
						break;
					case AssetType.InSceneAndAssets:
						assets = true;
						scenes = true;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(assetType), assetType, null);
				}

				if (scenes)
				{
					var obj = Object.FindObjectOfType(type);
					if (obj != null)
						return obj;
				}

				if (assets)
				{
					GameObject[] prefabs = EditorUtils.LoadAssetsOfType<GameObject>();
					foreach (GameObject prefab in prefabs)
						if (prefab.TryGetComponent(type, out var obj))
							return obj;
				}

				return null;
			}

			string[] guids = AssetDatabase.FindAssets($"t:{type.FullName}", filterArray);
			if (guids.Length == 0)
			{
				guids = AssetDatabase.FindAssets($"t:{type.Name}", filterArray);
				if (guids.Length == 0)
					return null;
			}

			foreach (string guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type);
				if (asset != null)
					return asset;
			}

			return null;
		}

		public static List<Object> LoadAssetsByTypeName(
			string assemblyQualifiedName,
			out Type type,
			out bool isComponent,
			AssetType assetType)
		{
			type = Type.GetType(assemblyQualifiedName);
			isComponent = false;

			List<Object> values = new List<Object>();
			if (type == null)
				return values;

			isComponent = type.IsSubclassOf(typeof(Component));
			if (isComponent)
			{
				bool assets = false, scenes = false;
				switch (assetType)
				{
					case AssetType.InAssets:
						assets = true;
						break;
					case AssetType.InScene:
						scenes = true;
						break;
					case AssetType.InSceneAndAssets:
						assets = true;
						scenes = true;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(assetType), assetType, null);
				}

				if (assets)
				{
					GameObject[] prefabs = EditorUtils.LoadAssetsOfType<GameObject>();
					values = new List<Object>();
					foreach (GameObject prefab in prefabs)
						values.AddRange(prefab.GetComponents(type));
				}

				if (scenes)
					values.AddRange(Object.FindObjectsOfType(type));

				return values;
			}

			string[] guids = AssetDatabase.FindAssets($"t:{type.FullName}", filterArray);
			if (guids.Length == 0)
			{
				guids = AssetDatabase.FindAssets($"t:{type.Name}", filterArray);
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
					if (property.stringValue == string.Empty && property.propertyPath == "m_Name")
						return property.serializedObject.targetObject.name;
					return property.stringValue;
				case SerializedPropertyType.ObjectReference:
					return property.objectReferenceValue == null ? string.Empty : property.objectReferenceValue.name;
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

		public static void DrawTextureInRect(Rect r, Texture texture)
		{
			float h = r.height - 2;
			Rect textureRect;
			if (texture.height == texture.width)
				textureRect = new Rect(r.x, r.y, h, h);
			else if (texture.height > texture.width)
			{
				float width = h * (texture.width / (float) texture.height);
				float missingWidth = h - width;
				textureRect = new Rect(r.x + missingWidth / 2f, r.y, width, h);
			}
			else
			{
				float height = h * (texture.height / (float) texture.width);
				float missingHeight = h - height;
				textureRect = new Rect(r.x, r.y + missingHeight / 2f, h, height);
			}

			GUI.DrawTexture(textureRect, texture);
		}

		public static bool IsValidPropertyKeyType(SerializedPropertyType propertyType)
		{
			switch (propertyType)
			{
				case SerializedPropertyType.Integer:
				case SerializedPropertyType.Boolean:
				case SerializedPropertyType.Float:
				case SerializedPropertyType.String:
				case SerializedPropertyType.LayerMask:
				case SerializedPropertyType.Enum:
				case SerializedPropertyType.Character:
					return true;
				default:
					return false;
			}
		}
	}
}