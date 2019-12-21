using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Vertx.Editor
{
	public class ObjectSelectionPopup : EditorWindow
	{
		private SerializedProperty propertyTarget;

		private class ObjectInfo
		{
			public enum InfoType
			{
				SceneComponent,
				PrefabAssetComponent,
				ScriptableObject,
			}

			public InfoType ObjectInfoType { get; }
			public Object Object { get; private set; }
			public string GUID { get; }
			public Type Type { get; }

			/// <summary>
			/// Constructor for Scenes.
			/// </summary>
			public ObjectInfo(Object o, Type type)
			{
				Object = o;
				Type = type;
				ObjectInfoType = InfoType.SceneComponent;
			}

			/// <summary>
			/// Constructor for Assets
			/// </summary>
			public ObjectInfo(string guid, Type type, bool isPrefab)
			{
				GUID = guid;
				Type = type;
				ObjectInfoType = isPrefab ? InfoType.PrefabAssetComponent : InfoType.ScriptableObject;
			}

			public void ResolveObjectFromGUID()
			{
				switch (ObjectInfoType)
				{
					case InfoType.SceneComponent:
						return;
					case InfoType.ScriptableObject:
						Object = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(GUID), Type);
						return;
					case InfoType.PrefabAssetComponent:
						GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GUID));
						if (Type == typeof(GameObject))
						{
							Object = prefab;
							return;
						}

						Object = prefab.GetComponent(Type);
						return;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private readonly List<ObjectInfo> results = new List<ObjectInfo>();

		[Flags]
		private enum ResultType
		{
			Asset = 0,
			Scene = 1,
			Packages = 1 << 1
		}

		private ResultType resultType;
		private bool forceIgnoreAssets;
		private const string packageResultKey = "Vertx_SearchPackages";

		private enum ObjectType
		{
			GameObject,
			Component,
			Asset
		}

		public void Initialise(SerializedProperty property, bool ignoreAssets = false)
		{
			propertyTarget = property;
			results.Clear();
			Object rootObject = property.serializedObject.targetObject;

			resultType = 0;

			//Set up ResultType enum---------------------
			forceIgnoreAssets = ignoreAssets;
			if (!ignoreAssets)
			{
				resultType = ResultType.Asset;
				if (EditorPrefs.GetBool(packageResultKey, true))
					resultType |= ResultType.Packages;
			}

			//Initialise information about the type
			bool isPersistent = EditorUtility.IsPersistent(rootObject);
			GetFieldInfoFromProperty(property, out Type type);
			Type gameObjectType = typeof(GameObject);
			ObjectType objectType;
			if (type == gameObjectType)
				objectType = ObjectType.GameObject;
			else if (type.IsSubclassOf(typeof(Component)))
				objectType = ObjectType.Component;
			else
				objectType = ObjectType.Asset;

			if (!isPersistent)
			{
				switch (objectType)
				{
					case ObjectType.GameObject:
					case ObjectType.Component:
						resultType |= ResultType.Scene;
						break;
					case ObjectType.Asset:
						//Ignore searching the scene for any type that cannot be in it
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			//------------------------------------------


			if (resultType == 0)
			{
				Debug.LogError($"Results does not include any valid locations for type {type}. Ignoring assets: {ignoreAssets}.");
				Close();
				return;
			}

			if (resultType.HasFlag(ResultType.Asset))
			{
				string[] locations =
					resultType.HasFlag(ResultType.Packages) ? new[] {"Assets", "Packages"} : new[] {"Assets"};
				RetrieveResults();

				void RetrieveResults()
				{
					switch (objectType)
					{
						case ObjectType.Component:
							//Components have to be found on the root of prefabs
							string[] guids = AssetDatabase.FindAssets("t:GameObject", locations);
							foreach (string guid in guids)
							{
								string path = AssetDatabase.GUIDToAssetPath(guid);
								if (AssetDatabase.LoadAssetAtPath<GameObject>(path).TryGetComponent(type, out _))
									results.Add(new ObjectInfo(guid, type, true));
							}

							break;
						case ObjectType.GameObject:
						case ObjectType.Asset:
							//Collect asset types directly
							results.AddRange(AssetDatabase.FindAssets($"t:{type.FullName}", locations)
								.Select(guid => new ObjectInfo(guid, type, objectType == ObjectType.GameObject))
							);
							return;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}

			if (resultType.HasFlag(ResultType.Scene))
			{
				Component rootComponent = (Component) rootObject;
				Scene scene = rootComponent.gameObject.scene;
				List<GameObject> allGameObjectsInScene = GetAllGameObjectsInScene(scene);
				if (type == gameObjectType)
				{
					foreach (GameObject gameObject in allGameObjectsInScene)
						results.Add(new ObjectInfo(gameObject, gameObjectType));
				}
				else
				{
					foreach (GameObject gameObject in allGameObjectsInScene)
					{
						if (gameObject.TryGetComponent(type, out Component component))
							results.Add(new ObjectInfo(component, type));
					}
				}
			}

			Debug.Log(results.Count);
			CreateUI();
		}

		public static List<GameObject> GetAllGameObjectsInScene(Scene scene)
		{
			List<GameObject> gameObjects = new List<GameObject>();
			scene.GetRootGameObjects(gameObjects);
			for (int i = 0; i < gameObjects.Count; i++)
			{
				GameObject gameObject = gameObjects[i];
				GetChildren(gameObject.transform);

				void GetChildren(Transform root)
				{
					for (int j = 0; j < root.childCount; j++)
					{
						Transform child = root.GetChild(j);
						gameObjects.Add(child.gameObject);
						GetChildren(child);
					}
				}
			}

			return gameObjects;
		}

		private void CreateUI()
		{
			Toolbar toolbar = new Toolbar();
			rootVisualElement.Add(toolbar);

			ToolbarSearchField toolbarSearchField = new ToolbarSearchField();
			toolbar.Add(toolbarSearchField);


			ListView listView = new ListView(results,
				//height
				(int) EditorGUIUtility.singleLineHeight,
				//Create
				() =>
				{
					VisualElement root = new VisualElement();
					
					return root;
				},
				//bind
				(element, i) => { })
			{
				selectionType = SelectionType.Single
			};
			listView.onItemChosen += o =>
			{
				var objectInfo = (ObjectInfo) o;
				objectInfo.ResolveObjectFromGUID();
				propertyTarget.objectReferenceValue = objectInfo.Object;
				propertyTarget.serializedObject.ApplyModifiedProperties();
				Close();
			};
			rootVisualElement.Add(listView);
		}

		#region Helpers

		private static MethodInfo getFieldInfoFromPropertyMI;

		private static MethodInfo GetFieldInfoFromPropertyMI =>
			getFieldInfoFromPropertyMI ?? (getFieldInfoFromPropertyMI =
				typeof(EditorWindow).Assembly.GetType("UnityEditor.ScriptAttributeUtility").GetMethod("GetFieldInfoFromProperty", BindingFlags.NonPublic | BindingFlags.Static));

		public static FieldInfo GetFieldInfoFromProperty(SerializedProperty property, out Type type)
		{
			object[] parameters = new object[2];
			parameters[0] = property;
			FieldInfo fieldInfo = (FieldInfo) GetFieldInfoFromPropertyMI.Invoke(null, parameters);
			type = (Type) parameters[1];
			return fieldInfo;
		}

		#endregion
	}
}