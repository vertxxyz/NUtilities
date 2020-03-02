using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
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

			HashSet<string> loadedPaths = new HashSet<string>();
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (loadedPaths.Contains(path))
					continue;
				loadedPaths.Add(path);
				var asset = AssetDatabase.LoadAllAssetsAtPath(path);
				foreach (Object o in asset)
				{
					if (type.IsInstanceOfType(o))
						values.Add(o);
				}
			}

			return values;
		}

		public static string GetValueForRegex(SerializedProperty property)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
					return property.intValue.ToString();
				case SerializedPropertyType.Boolean:
					return property.boolValue.ToString();
				case SerializedPropertyType.Float:
					return property.floatValue.ToString(CultureInfo.InvariantCulture);
				case SerializedPropertyType.String:
					if (property.stringValue == string.Empty && property.propertyPath == "m_Name")
						return property.serializedObject.targetObject.name;
					return property.stringValue;
				case SerializedPropertyType.ObjectReference:
					return property.objectReferenceValue == null ? string.Empty : property.objectReferenceValue.name;
				case SerializedPropertyType.LayerMask:
					return property.enumDisplayNames[property.intValue];
				case SerializedPropertyType.Enum:
					return property.enumDisplayNames[property.intValue];
				case SerializedPropertyType.ArraySize:
					return property.arraySize.ToString();
				case SerializedPropertyType.Character:
					return property.stringValue;
				//Values below this point are sorted completely arbitrarily
				case SerializedPropertyType.Color:
					return property.colorValue.ToString();
				case SerializedPropertyType.Vector2:
					return property.vector2Value.ToString();
				case SerializedPropertyType.Vector3:
					return property.vector3Value.ToString();
				case SerializedPropertyType.Vector4:
					return property.vector4Value.ToString();
				case SerializedPropertyType.Quaternion:
					return property.quaternionValue.eulerAngles.ToString();
				case SerializedPropertyType.FixedBufferSize:
					return property.fixedBufferSize.ToString();
				case SerializedPropertyType.Vector2Int:
					return property.vector2IntValue.ToString();
				case SerializedPropertyType.Vector3Int:
					return property.vector3IntValue.ToString();
				case SerializedPropertyType.Rect:
					return property.rectValue.ToString();
				case SerializedPropertyType.RectInt:
					return property.rectIntValue.ToString();
				case SerializedPropertyType.Bounds:
					return property.boundsValue.ToString();
				case SerializedPropertyType.BoundsInt:
					return property.boundsIntValue.ToString();
				case SerializedPropertyType.ExposedReference:
					return property.exposedReferenceValue.name;
				case SerializedPropertyType.Gradient:
					Gradient gradient = GetGradientValue(property);
					if (gradient == null) return string.Empty;
					StringBuilder sB = new StringBuilder(20);
					string asciiGradient = " .:-=+*#%@";
					int gradientMultiplier = asciiGradient.Length - 1;
					for (int i = 0; i < 20; i++)
					{
						float grayscale = 1 - gradient.Evaluate(i / 19f).grayscale;
						sB.Append(asciiGradient[Mathf.Clamp(Mathf.RoundToInt(grayscale * gradientMultiplier), 0, gradientMultiplier)]);
					}
					return sB.ToString();
				case SerializedPropertyType.AnimationCurve:
				#if UNITY_2019_3_OR_NEWER
				case SerializedPropertyType.ManagedReference:
				#endif
				case SerializedPropertyType.Generic:
				default:
					throw new ArgumentOutOfRangeException($"{property.propertyType} is not supported by {nameof(GetValueForRegex)}");
			}

			Gradient GetGradientValue(SerializedProperty sp)
			{
				PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty(
					"gradientValue",
					BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
					null,
					typeof(Gradient),
					Array.Empty<Type>(),
					null
				);
				return propertyInfo?.GetValue(sp, null) as Gradient;
			}
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
				#if UNITY_2019_3_OR_NEWER
				case SerializedPropertyType.ManagedReference:
				#endif
				case SerializedPropertyType.Gradient:
				case SerializedPropertyType.Generic:
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static float GetMinWidth(AssetListConfiguration.ColumnConfiguration column)
		{
			SerializedPropertyType propertyType = column.IsArray ? column.ArrayPropertyInformation.ArrayPropertyType : column.PropertyType;

			float minWidth;
			switch (propertyType)
			{
				case SerializedPropertyType.Float:
				case SerializedPropertyType.Integer:
					switch (column.NumericalDisplay)
					{
						case NumericalPropertyDisplay.ReadonlyProgressBar:
						case NumericalPropertyDisplay.ReadonlyProgressBarNormalised:
							minWidth = 150;
							break;
						default:
							minWidth = 50;
							break;
					}

					break;
				case SerializedPropertyType.Enum:
					switch (column.EnumDisplay)
					{
						case EnumPropertyDisplay.Property:
						case EnumPropertyDisplay.ReadonlyProperty:
							minWidth = 150;
							break;
						case EnumPropertyDisplay.ReadonlyLabel:
							minWidth = 80;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					break;
				case SerializedPropertyType.String:
					minWidth = 150;
					break;
				case SerializedPropertyType.Color:
					minWidth = 150;
					break;
				case SerializedPropertyType.ObjectReference:
					minWidth = 180;
					break;
				case SerializedPropertyType.Generic:
					throw new ArgumentException($"Generic Property Types cannot be accepted by the {nameof(GetMinWidth)} function.");
				case SerializedPropertyType.Boolean:
				case SerializedPropertyType.LayerMask:
				case SerializedPropertyType.Vector2:
				case SerializedPropertyType.Vector3:
				case SerializedPropertyType.Vector4:
				case SerializedPropertyType.Rect:
				case SerializedPropertyType.ArraySize:
				case SerializedPropertyType.Character:
				case SerializedPropertyType.AnimationCurve:
				case SerializedPropertyType.Bounds:
				case SerializedPropertyType.Gradient:
				case SerializedPropertyType.Quaternion:
				case SerializedPropertyType.ExposedReference:
				case SerializedPropertyType.FixedBufferSize:
				case SerializedPropertyType.Vector2Int:
				case SerializedPropertyType.Vector3Int:
				case SerializedPropertyType.RectInt:
				case SerializedPropertyType.BoundsInt:
				#if UNITY_2019_3_OR_NEWER
				case SerializedPropertyType.ManagedReference:
				#endif
				default:
					minWidth = 200;
					break;
			}

			return minWidth;
		}

		public static string GetPropertyDisplayString(SerializedPropertyType propertyType)
		{
			string propertyName = null;
			switch (propertyType)
			{
				case SerializedPropertyType.Generic:
					throw new ArgumentException($"{SerializedPropertyType.Generic} cannot be handled by {nameof(GetPropertyDisplayString)}");
				case SerializedPropertyType.Integer:
				case SerializedPropertyType.Float:
					propertyName = "NumericalDisplay";
					break;
				case SerializedPropertyType.Boolean:
					break;
				case SerializedPropertyType.String:
					propertyName = "StringDisplay";
					break;
				case SerializedPropertyType.Color:
					propertyName = "ColorDisplay";
					break;
				case SerializedPropertyType.ObjectReference:
					propertyName = "ObjectDisplay";
					break;
				case SerializedPropertyType.LayerMask:
				case SerializedPropertyType.Enum:
					propertyName = "EnumDisplay";
					break;
				case SerializedPropertyType.Vector2:
				case SerializedPropertyType.Vector3:
				case SerializedPropertyType.Vector4:
				case SerializedPropertyType.Rect:
				case SerializedPropertyType.ArraySize:
				case SerializedPropertyType.Character:
				case SerializedPropertyType.AnimationCurve:
				case SerializedPropertyType.Bounds:
				case SerializedPropertyType.Gradient:
				case SerializedPropertyType.Quaternion:
				case SerializedPropertyType.ExposedReference:
				case SerializedPropertyType.FixedBufferSize:
				case SerializedPropertyType.Vector2Int:
				case SerializedPropertyType.Vector3Int:
				case SerializedPropertyType.RectInt:
				case SerializedPropertyType.BoundsInt:
				#if UNITY_2019_3_OR_NEWER
				case SerializedPropertyType.ManagedReference:
				#endif
				default:
					propertyName = "DefaultDisplay";
					break;
			}

			return propertyName;
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