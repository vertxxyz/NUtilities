using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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

		/// <summary>
		/// Runs a function on all components, ignoring the Transform.
		/// </summary>
		/// <param name="componentAction">Action to run on a component</param>
		protected static void RunFunctionOnSceneObjects(Action<Component> componentAction)
			=> RunFunctionOnSceneObjects(RunOnComponentsUnderRootGameObjectIgnoringTransform, componentAction);
		
		/// <summary>
		/// Runs a function on all components of type T.
		/// </summary>
		/// <param name="componentAction">Action to run on a component</param>
		/// <typeparam name="T">The type of component associated with the action</typeparam>
		protected static void RunFunctionOnSceneObjects<T>(Action<T> componentAction) where T : Component
			=> RunFunctionOnSceneObjects(RunOnComponentsUnderRootGameObject, componentAction);

		private static void RunFunctionOnSceneObjects<T>(Action<GameObject, Action<T>> rootAction, Action<T> componentAction) where T : Component
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
					rootAction(rootGameObject, componentAction);
				}
			}
		}
		
		protected static void RunFunctionOnSceneRootGameObjects(Action<GameObject> rootAction)
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
					rootAction(rootGameObject);
				}
			}
		}
		
		protected static void RunOnComponentsUnderRootGameObjectIgnoringTransform(GameObject gameObject, Action<Component> componentAction)
		{
			Component[] components = gameObject.GetComponentsInChildren<Component>(true);
			foreach (Component component in components)
			{
				if (component == null) //Ignore unassigned components
					continue;

				//Skip transforms
				if(component.GetType() == transformType)
					continue;

				componentAction(component);
			}
		}
		
		protected static void RunOnComponentsUnderRootGameObject<T>(GameObject gameObject, Action<T> componentAction) where T : Component
		{
			T[] components = gameObject.GetComponentsInChildren<T>(true);
			foreach (T component in components)
				componentAction(component);
		}

		protected static void RunFunctionOnAssets(Action<GameObject> prefabAction, Action<ScriptableObject> scriptableObjectAction)
		{
			float progressTotal;
			if (prefabAction != null)
			{
				string[] guids = AssetDatabase.FindAssets($"t:{nameof(GameObject)}");
				int length = guids.Length;
				progressTotal = length - 1;
				for (var i = 0; i < length; i++)
				{
					string guid = guids[i];
					string path = AssetDatabase.GUIDToAssetPath(guid);
					if (!IsValidPath(path)) continue;
					EditorUtility.DisplayProgressBar("Checking Prefab Assets for missing references.", path, i / progressTotal);
					GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					prefabAction(prefab);
				}
			}

			if (scriptableObjectAction != null)
			{
				string[] guidsSO = AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}");
				int lengthSO = guidsSO.Length;

				progressTotal = lengthSO - 1;
				for (var i = 0; i < lengthSO; i++)
				{
					string guid = guidsSO[i];
					string path = AssetDatabase.GUIDToAssetPath(guid);
					if (!IsValidPath(path)) continue;
					EditorUtility.DisplayProgressBar("Checking ScriptableObject Assets for missing references.", path, i / progressTotal);
					ScriptableObject scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
					if (scriptableObject == null) continue; //This can occur if an asset has been created but the type is not compiled for the version.
					scriptableObjectAction(scriptableObject);
				}
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
	}
}