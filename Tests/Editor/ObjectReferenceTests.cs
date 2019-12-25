using System;
using UnityEngine;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Vertx.Testing.Editor
{
	public class ObjectReferenceTests
	{
		[Test]
		public void CheckForMissingReferencesInBuildScenes()
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
					CheckForMissingReferencesOnRootGameObject(rootGameObject);
				}
			}
		}
		
		[Test]
		public void CheckForMissingReferencesInAssets()
		{
			string[] guids = AssetDatabase.FindAssets($"t:{nameof(GameObject)}");
			int length = guids.Length;
			var progressTotal = (float) (length - 1);
			for (var i = 0; i < length; i++)
			{
				string guid = guids[i];
				string path = AssetDatabase.GUIDToAssetPath(guid);
				EditorUtility.DisplayProgressBar("Checking Assets for missing references.", path, i / progressTotal);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				CheckForMissingReferencesOnRootGameObject(prefab);
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

		public static void CheckForMissingReferencesOnRootGameObject(GameObject gameObject)
		{
			Component[] components = gameObject.GetComponentsInChildren<Component>();
			foreach (Component component in components)
			{
				//Skip transforms
				if(component.GetType() == transformType)
					continue;
				
				SerializedObject serializedObject = new SerializedObject(component);
				SerializedProperty property = serializedObject.GetIterator();
				while (property.NextVisible(true))
				{
					if (property.propertyType != SerializedPropertyType.ObjectReference)
						continue;
					if (property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0)
						continue;
					string path = $"{gameObject.name}/{AnimationUtility.CalculateTransformPath(component.transform, gameObject.transform)}";
					Assert.Fail($"{path}/{component.GetType()}.{property.propertyPath}\nWas found to be missing.");
				}
			}
		}
	}
}