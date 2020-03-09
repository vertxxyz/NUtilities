using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vertx.Extensions;

namespace Vertx.Editor
{
	// ReSharper disable once UnusedType.Global
	internal static class CleanupExtensions
	{
		[ActionProvider]
		private static ActionOperation CleanupUnreferencedMaterialProperties()
		{
			Material[] materials = Selection.GetFiltered<Material>(SelectionMode.Editable);
			return new ActionOperation("Cleanup Unreferenced Properties", "Material", () => materials.Length > 0, () =>
			{
				if(materials.Length == 0)
					materials = EditorUtils.LoadAssetsOfType<Material>();

				int count = 0;

				Dictionary<Shader, HashSet<string>> properties = new Dictionary<Shader, HashSet<string>>();
				for (var index = 0; index < materials.Length; index++)
				{
					Material material = materials[index];
					if (EditorUtility.DisplayCancelableProgressBar("Cleaning Unreferenced Material Properties", material.name, index / (float)materials.Length))
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
			});
		}
	}
}