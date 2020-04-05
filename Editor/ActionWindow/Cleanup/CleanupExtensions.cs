using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vertx.Extensions;
using Object = UnityEngine.Object;

namespace Vertx.Editor
{
	// ReSharper disable once UnusedType.Global
	internal static class CleanupExtensions
	{
		#region Materials

		private const string cleanupMaterialsTitle = "Material - Cleanup Unreferenced Properties";

		[ActionProvider]
		private static IEnumerable<ActionOperation> CleanupUnreferencedMaterialProperties()
		{
			//Operate On Selection
			Material[] selectedMaterials = Selection.GetFiltered<Material>(SelectionMode.Editable);
			if (selectedMaterials.Length > 0)
			{
				yield return new ActionOperation(ActionOperation.ActionTarget.Selection,
					cleanupMaterialsTitle,
					"Material", () => CleanupUnreferencedMaterialProperties(selectedMaterials));
			}

			//All materials
			yield return new ActionOperation(ActionOperation.ActionTarget.All,
				cleanupMaterialsTitle,
				"Material", () =>
				{
					Material[] materials = EditorUtils.LoadAssetsOfType<Material>();
					CleanupUnreferencedMaterialProperties(materials);
				});
		}

		private static void CleanupUnreferencedMaterialProperties(Material[] materials)
		{
			int count = 0;

			Dictionary<Shader, HashSet<string>> properties = new Dictionary<Shader, HashSet<string>>();
			for (var index = 0; index < materials.Length; index++)
			{
				Material material = materials[index];
				if (EditorUtility.DisplayCancelableProgressBar("Cleaning Unreferenced Material Properties", material.name, index / (float) materials.Length))
					break;
				Shader shader = material.shader;
				if (shader == null) continue; //Ignore unassigned shaders
				try
				{
					if (!properties.TryGetValue(shader, out var propertyNames))
					{
						propertyNames = new HashSet<string>();
						int propertyCount = shader.GetPropertyCount();
						for (int i = 0; i < propertyCount; i++)
							propertyNames.Add(shader.GetPropertyName(i));
						properties.Add(shader, propertyNames);
					}

					using (var sO = new SerializedObject(material))
					{
						SerializedProperty texEnvs = sO.FindProperty("m_SavedProperties.m_TexEnvs");
						SerializedProperty floats = sO.FindProperty("m_SavedProperties.m_Floats");
						SerializedProperty colors = sO.FindProperty("m_SavedProperties.m_Colors");

						bool changed = false;
						Validate(texEnvs);
						Validate(floats);
						Validate(colors);

						void Validate(SerializedProperty array)
						{
							for (int i = 0; i < array.arraySize; i++)
							{
								SerializedProperty element = array.GetArrayElementAtIndex(i);
								SerializedProperty key = element.FindPropertyRelative("first");
								if (propertyNames.Contains(key.stringValue))
									continue; //Key was found

								array.DeleteArrayElementAtIndex(i--);
								changed = true;
							}
						}

						if (changed)
						{
							sO.ApplyModifiedPropertiesWithoutUndo();
							count++;
						}
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			Debug.Log($"{count} materials were modified.");
			if (count > 0)
				AssetDatabase.Refresh();
		}

		#endregion

		#region GameObjects

		private const string trimGameObjectNamesTitle = "GameObject - Trim (X) and (Clone) from names.";

		[ActionProvider]
		private static IEnumerable<ActionOperation> TrimGameObjectNamesOfDuplicateIndices()
		{
			//Operate On Selection
			GameObject[] selectedGameObjects = Selection.GetFiltered<GameObject>(SelectionMode.Editable);
			if (selectedGameObjects.Length > 0)
			{
				yield return new ActionOperation(
					ActionOperation.ActionTarget.Selection,
					trimGameObjectNamesTitle,
					"GameObject",
					() =>
					{
						int countReplaced = TrimGameObjectNamesOfDuplicateIndices(selectedGameObjects);
						Debug.Log($"Replaced {countReplaced} GameObject names in Selection");
					});
			}

			yield return new ActionOperation(
				ActionOperation.ActionTarget.Scene,
				trimGameObjectNamesTitle,
				"GameObject",
				() =>
				{
					for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
					{
						Scene scene = SceneManager.GetSceneAt(i);
						IEnumerable<GameObject> gameObjects = EditorUtils.GetAllGameObjectsInScene(scene);
						int countReplaced = TrimGameObjectNamesOfDuplicateIndices(gameObjects);
						Debug.Log($"Replaced {countReplaced} GameObject names in {scene.name}");
					}
				});
		}

		private static int TrimGameObjectNamesOfDuplicateIndices(IEnumerable<GameObject> gameObjects)
		{
			int countReplaced = 0;
			foreach (GameObject gameObject in gameObjects)
			{
				if (!gameObject.name.EndsWith(")")) continue;
				int lastIndex = gameObject.name.LastIndexOf(" (", StringComparison.Ordinal);
				if (lastIndex <= 0) continue;
				string value = gameObject.name.Substring(lastIndex + 2, gameObject.name.Length - (lastIndex + 2) - 1);
				if (int.TryParse(value, out int _) || value.Equals("Clone"))
				{
					Undo.RecordObject(gameObject, "Removed GameObject Indices");
					gameObject.name = gameObject.name.Remove(lastIndex);
					countReplaced++;
				}
			}

			return countReplaced;
		}

		#endregion

		#region Scripts

		private const string cleanupMissingComponentsTitle = "Component - Remove Missing Components";

		[ActionProvider]
		private static ActionOperation CleanupMissingScriptsComponentsInProject()
		{
			return new ActionOperation(ActionOperation.ActionTarget.All,
				cleanupMissingComponentsTitle,
				"Component", () =>
				{
					using (var scope = new EditorUtils.BuildSceneScope())
					{
						foreach (var scene in scope)
						{
							IEnumerable<GameObject> gameObjects = EditorUtils.GetAllGameObjectsInScene(scene);
							int countRemoved = RemoveMissingScriptsComponentsOnGameObjects(gameObjects);
							if (countRemoved > 0)
							{
								Debug.Log($"Removed {countRemoved} GameObject names in {scene.name}");
								EditorSceneManager.SaveScene(scene);
							}
						}
					}
				});
		}

		static int RemoveMissingScriptsComponentsOnGameObjects(IEnumerable<GameObject> gameObject)
		{
			int countRemoved = 0;
			foreach (GameObject gO in gameObject)
			{
				var components = gO.GetComponents<Component>();
				foreach (Component component in components)
				{
					if (component == null)
					{
						SerializedObject sO = new SerializedObject(gO);
						var componentsProp = sO.FindProperty("m_Component");
						for (int i = components.Length - 1; i >= 0; i--)
						{
							//If it's a prefab and connected to it, then we should be modifying the prefab instead.
							if(PrefabUtility.GetPrefabInstanceStatus(components[i]) != PrefabInstanceStatus.NotAPrefab) continue;
							if (components[i] == null)
							{
								componentsProp.DeleteArrayElementAtIndex(i);
								countRemoved++;
							}
						}

						sO.ApplyModifiedPropertiesWithoutUndo();
						break;
					}
				}
			}

			return countRemoved;
		}

		#endregion
	}
}