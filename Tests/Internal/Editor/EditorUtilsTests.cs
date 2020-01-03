using System;
using UnityEngine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Vertx.Extensions;
using Object = UnityEngine.Object;

namespace Vertx.Testing.Editor
{
	public class EditorUtilsTests
	{
		[Test]
		public void CheckPropertyFieldsInBuildScenes()
		{
			int buildSceneCount = SceneManager.sceneCountInBuildSettings;
			for (int buildIndex = 0; buildIndex < buildSceneCount; buildIndex++)
			{
				string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
				EditorSceneManager.OpenScene(path, buildIndex == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
				Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);

				string checkingLabel = $"Checking {scene.name} ({buildIndex + 1}/{buildSceneCount}) for GetObjectFromProperty failures.";

				GameObject[] rootGameObjects = scene.GetRootGameObjects();
				int length = rootGameObjects.Length;
				var progressTotal = (float) (length - 1);
				for (var i = 0; i < length; i++)
				{
					EditorUtility.DisplayProgressBar(checkingLabel, path, i / progressTotal);
					GameObject rootGameObject = rootGameObjects[i];
					CheckPropertyFieldsUnderRootGameObject(rootGameObject);
				}
			}
		}

		[Test]
		public void CheckPropertyFieldsInAssets()
		{
			string[] guids = AssetDatabase.FindAssets($"t:{nameof(GameObject)}");
			string[] guidsSO = AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}");
			int length = guids.Length;
			int lengthSO = guidsSO.Length;
			float progressTotal = length - 1;
			for (var i = 0; i < length; i++)
			{
				string guid = guids[i];
				string path = AssetDatabase.GUIDToAssetPath(guid);
				EditorUtility.DisplayProgressBar("Checking Prefab Assets for GetObjectFromProperty failures.", path, i / progressTotal);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				CheckPropertyFieldsUnderRootGameObject(prefab);
			}

			progressTotal = lengthSO - 1;
			for (var i = 0; i < lengthSO; i++)
			{
				string guid = guidsSO[i];
				string path = AssetDatabase.GUIDToAssetPath(guid);
				EditorUtility.DisplayProgressBar("Checking ScriptableObjects for GetObjectFromProperty failures.", path, i / progressTotal);
				ScriptableObject scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
				CheckPropertyFieldsOnObject(scriptableObject);
			}
		}

		private SceneSetup[] sceneManagerSetup;

		[SetUp]
		public void SetUp() => sceneManagerSetup = EditorSceneManager.GetSceneManagerSetup();

		[TearDown]
		public void TearDown()
		{
			EditorUtility.ClearProgressBar();
			if (sceneManagerSetup != null && sceneManagerSetup.Length > 0)
				EditorSceneManager.RestoreSceneManagerSetup(sceneManagerSetup);
		}

		private static readonly Type transformType = typeof(Transform);

		public static void CheckPropertyFieldsUnderRootGameObject(GameObject gameObject)
		{
			Component[] components = gameObject.GetComponentsInChildren<Component>();
			foreach (Component component in components)
			{
				Type componentType = component.GetType();
				//Skip transforms
				if (componentType == transformType)
					continue;

				CheckPropertyFieldsOnObject(component);
			}
		}

		static bool CheckEquality<T>(object objectFromProperty, T compareTo)
		{
			if (compareTo.Equals((T) objectFromProperty))
				return true;
			Debug.Log($"{compareTo} != {(T) objectFromProperty}");
			return false;
		}

		static bool CheckObjectEquality(Object objectFromProperty, Object compareTo)
		{
			if (compareTo == objectFromProperty)
				return true;
			Debug.Log($"{compareTo} != {objectFromProperty}");
			return false;
		}

		private static void CheckPropertyFieldsOnObject(Object @object)
		{
			SerializedObject serializedObject = new SerializedObject(@object);
			SerializedProperty property = serializedObject.GetIterator();
			while (property.NextVisible(true))
			{
				if (property.propertyPath.EndsWith("m_Script")) continue;
				try
				{
					object objectFromProperty = EditorUtils.GetObjectFromProperty(property, out _, out _);

					switch (property.propertyType)
					{
						case SerializedPropertyType.ObjectReference:
							if (CheckObjectEquality((Object) objectFromProperty, property.objectReferenceValue))
								continue;
							break;
						case SerializedPropertyType.Integer:
							if (objectFromProperty is uint)
							{
								if (CheckEquality(objectFromProperty, (uint) property.intValue))
									continue;
							}
							else if (objectFromProperty is long)
							{
								if (CheckEquality(objectFromProperty, property.longValue))
									continue;
							}
							else if (objectFromProperty is byte)
							{
								if (CheckEquality(objectFromProperty, (byte) property.intValue))
									continue;
							}
							else if (objectFromProperty is short)
							{
								if (CheckEquality(objectFromProperty, (short) property.intValue))
									continue;
							}
							else
							{
								if (CheckEquality(objectFromProperty, property.intValue))
									continue;
							}
							break;
						case SerializedPropertyType.Enum:
							Type baseEnumType = Enum.GetUnderlyingType(objectFromProperty.GetType());
							if (baseEnumType == typeof(int))
							{
								if (CheckEquality(objectFromProperty, property.intValue))
									continue;
							}
							else if (baseEnumType == typeof(uint))
							{
								if (CheckEquality(objectFromProperty, (uint)property.intValue))
									continue;
							}
							else if (baseEnumType == typeof(byte))
							{
								if (CheckEquality(objectFromProperty, (byte)property.intValue))
									continue;
							}
							else if (baseEnumType == typeof(long))
							{
								if (CheckEquality(objectFromProperty, (long)property.intValue))
									continue;
							}
							else if (baseEnumType == typeof(short))
							{
								if (CheckEquality(objectFromProperty, (short)property.intValue))
									continue;
							}
							break;
						case SerializedPropertyType.Boolean:
							if (CheckEquality(objectFromProperty, property.boolValue))
								continue;
							break;
						case SerializedPropertyType.Float:
							if (objectFromProperty is double)
							{
								if (CheckEquality(objectFromProperty, property.doubleValue))
									continue;
							}
							else
							{
								if (CheckEquality(objectFromProperty, property.floatValue))
									continue;
							}
							break;
						case SerializedPropertyType.String:
							if(objectFromProperty == null && string.IsNullOrEmpty(property.stringValue))
								continue;
							if (property.stringValue.Equals((string)objectFromProperty, StringComparison.Ordinal))
								continue;
							Debug.Log($"{property.stringValue} != {objectFromProperty}");
							break;
						case SerializedPropertyType.Color:
							if (objectFromProperty is Color)
							{
								if (CheckEquality(objectFromProperty, property.colorValue))
									continue;
							}
							else
							{
								if (CheckEquality<Color32>(objectFromProperty, property.colorValue))
									continue;
							}
							break;
						case SerializedPropertyType.LayerMask:
							if (CheckEquality<LayerMask>(objectFromProperty, property.intValue))
								continue;
							break;
						case SerializedPropertyType.Vector2:
							if (CheckEquality(objectFromProperty, property.vector2Value))
								continue;
							break;
						case SerializedPropertyType.Vector3:
							if (CheckEquality(objectFromProperty, property.vector3Value))
								continue;
							break;
						case SerializedPropertyType.Vector4:
							if (CheckEquality(objectFromProperty, property.vector4Value))
								continue;
							break;
						case SerializedPropertyType.Rect:
							if (CheckEquality(objectFromProperty, property.rectValue))
								continue;
							break;
						case SerializedPropertyType.ArraySize:
							if (CheckEquality(objectFromProperty, property.intValue))
								continue;
							break;
						case SerializedPropertyType.Character:
							if (CheckEquality(objectFromProperty, (char) property.intValue))
								continue;
							break;
						case SerializedPropertyType.AnimationCurve:
							if (CheckEquality(objectFromProperty, property.animationCurveValue))
								continue;
							break;
						case SerializedPropertyType.Bounds:
							if (CheckEquality(objectFromProperty, property.boundsValue))
								continue;
							break;
						case SerializedPropertyType.Quaternion:
							if (CheckEquality(objectFromProperty, property.quaternionValue))
								continue;
							break;
						case SerializedPropertyType.ExposedReference:
							if (CheckObjectEquality((Object) objectFromProperty, property.exposedReferenceValue))
								continue;
							break;
						case SerializedPropertyType.FixedBufferSize:
							if (CheckEquality(objectFromProperty, property.fixedBufferSize))
								continue;
							break;
						case SerializedPropertyType.Vector2Int:
							if (CheckEquality(objectFromProperty, property.vector2IntValue))
								continue;
							break;
						case SerializedPropertyType.Vector3Int:
							if (CheckEquality(objectFromProperty, property.vector3IntValue))
								continue;
							break;
						case SerializedPropertyType.RectInt:
							if (CheckEquality(objectFromProperty, property.rectIntValue))
								continue;
							break;
						case SerializedPropertyType.BoundsInt:
							if (CheckEquality(objectFromProperty, property.boundsIntValue))
								continue;
							break;
						case SerializedPropertyType.Gradient:
						case SerializedPropertyType.Generic:
							continue;
						default:
							continue;
					}
				}
				catch (Exception e)
				{
					if (e is FieldAccessException)
						continue;
					Log(e);
					throw;
				}

				Log();

				void Log(Exception exception = null)
				{
					string path = EditorUtility.IsPersistent(@object) ? $"{AssetDatabase.GetAssetPath(@object)}/" : string.Empty;
					if (@object is Component component)
					{
						Transform transform = component.transform;
						string tPath = AnimationUtility.CalculateTransformPath(transform, transform.root);
						path += string.IsNullOrEmpty(tPath) ? component.ToString() : $"{tPath}/{component}";
					}
					else
						path += @object.ToString();

					Assert.Fail($"{path}.{property.propertyPath}\nFailed {nameof(EditorUtils.GetObjectFromProperty)}.\n{exception}");
				}
			}
		}
	}
}