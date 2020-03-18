using System;
using UnityEngine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Vertx.Testing.Editor
{
	public abstract class ReferenceTests
	{
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

		protected static void RunFunctionOnSceneObjects(Action<Component> componentAction)
		{
			int buildSceneCount = SceneManager.sceneCountInBuildSettings;
			for (int buildIndex = 0; buildIndex < buildSceneCount; buildIndex++)
			{
				string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
				EditorSceneManager.OpenScene(path, buildIndex == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
				Scene scene = SceneManager.GetSceneByBuildIndex(buildIndex);

				string checkingLabel = $"Checking {scene.name} ({buildIndex + 1}/{buildSceneCount}) for missing references.";
				
				GameObject[] rootGameObjects = scene.GetRootGameObjects();
				int length = rootGameObjects.Length;
				var progressTotal = (float) (length - 1);
				for (var i = 0; i < length; i++)
				{
					EditorUtility.DisplayProgressBar(checkingLabel, path, i / progressTotal);
					GameObject rootGameObject = rootGameObjects[i];
					CheckForMissingReferencesUnderRootGameObject(rootGameObject, componentAction);
				}
			}
		}
		
		protected static void CheckForMissingReferencesUnderRootGameObject(GameObject gameObject, Action<Component> componentAction)
		{
			Component[] components = gameObject.GetComponentsInChildren<Component>();
			foreach (Component component in components)
			{
				//Skip transforms
				if(component.GetType() == transformType)
					continue;

				componentAction(component);
			}
		}

		protected static void RunFunctionOnAssets(Action<GameObject> prefabAction, Action<ScriptableObject> scriptableObjectAction)
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
				if(!IsValidPath(path)) continue;
				EditorUtility.DisplayProgressBar("Checking Prefab Assets for missing references.", path, i / progressTotal);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				prefabAction(prefab);
			}
			
			progressTotal = lengthSO - 1;
			for (var i = 0; i < lengthSO; i++)
			{
				string guid = guidsSO[i];
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if(!IsValidPath(path)) continue;
				EditorUtility.DisplayProgressBar("Checking ScriptableObject Assets for missing references.", path, i / progressTotal);
				ScriptableObject scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
				if(scriptableObject == null) continue; //This can occur if an asset has been created but the type is not compiled for the version.
				scriptableObjectAction(scriptableObject);
			}


			bool IsValidPath(string path)
			{
				//At the moment, all assets not in packages are valid.
				if (!path.StartsWith("Packages"))
					return true;
				
				//Ignore Unity packages. We cannot fix these.
				if (path.StartsWith("Packages/com.unity"))
					return false;
				//If someone has problems with this and wants to ignore all non-local packages, add this define.
				#if VERTX_TESTING_IGNORE_REMOTE_PACKAGES
				//Ignore any non-local packages.
				if (Path.GetFullPath(path).Contains("PackageCache"))
					return false;
				#endif
				return true;
			}
		}

		protected static string GetPathForObject(Object @object)
		{
			string path = EditorUtility.IsPersistent(@object) ? $"{AssetDatabase.GetAssetPath(@object)}/" : string.Empty;
			if (@object is Component component)
			{
				Transform transform = component.transform;
				string tPath = AnimationUtility.CalculateTransformPath(transform, null);
				var scene = component.gameObject.scene;
				if(scene.IsValid())
					path += $"({scene.path}) ";
				path += string.IsNullOrEmpty(tPath) ? component.ToString() : $"{tPath}/{component}";
			}
			else
				path += @object.ToString();

			return path;
		}
	}
	
	public class ObjectReferenceTests : ReferenceTests
	{
		[Test]
		public void CheckForMissingReferencesInBuildScenes()
			=> RunFunctionOnSceneObjects(CheckForMissingReferencesOnObject);

		[Test]
		public void CheckForMissingReferencesInAssets()
			=> RunFunctionOnAssets(CheckForMissingReferencesOnObject, CheckForMissingReferencesOnObject);

		public static void CheckForMissingReferencesOnObject (Object @object)
		{
			SerializedObject serializedObject = new SerializedObject(@object);
			SerializedProperty property = serializedObject.GetIterator();
			while (property.NextVisible(true))
			{
				if (property.propertyType != SerializedPropertyType.ObjectReference)
					continue;
				if (property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0)
					continue;
				string path = GetPathForObject(@object);

				Assert.Fail($"{path}.{property.propertyPath}\nWas found to be missing.");	
			}
		}
	}
}