#define OVERWRITE_SOURCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Vertx.Controls;

namespace Vertx.Extensions
{
	internal class MaterialPropertyRemapper : EditorWindow
	{
		[MenuItem("Window/Vertx/Material Property Remapper")]
		private static void OpenWindow()
		{
			var window = GetWindow<MaterialPropertyRemapper>();
			window.titleContent = new GUIContent("Remapper", EditorGUIUtility.ObjectContent(null, typeof(Material)).image);
			window.Show();
		}

		private enum RemapType
		{
			None,
			Texture,
			Float,
			VectorOrColor
		}

		private const string none = "None";

		private VisualElement remappingRoot;
		private const string fromKeyLabel = "From Key";
		private const string toKeyLabel = "To Key";

		private const string remapTypeName = "RemapTypePopup";
		private const string toKeyPopupName = "ToKeyPopup";
		private const string fromKeyPopupName = "FromKeyPopup";
		private const string remapButtonName = "RemapButton";
		private readonly List<string> noneList = new List<string> {none};

		private Dictionary<Shader, List<SerializedObject>> shadersToMaterials;

		private void OnEnable()
		{
			rootVisualElement.styleSheets.Add(StyleExtensions.GetStyleSheet("VertxShared"));
			rootVisualElement.AddToClassList("marginBorder");

			rootVisualElement.Add(new DragAndDropBox<Material>("Place Material Here", OnMaterialsDropped));
			rootVisualElement.Add(remappingRoot = new VisualElement());
		}

		private void OnDestroy() => DisposeOfSerializedObjects();

		void DisposeOfSerializedObjects()
		{
			if (shadersToMaterials == null)
				return;
			//Dispose of the old material SOs
			foreach (List<SerializedObject> value in shadersToMaterials.Values)
			{
				foreach (SerializedObject o in value)
					o.Dispose();
			}
		}

		private void OnMaterialsDropped(Material[] droppedMaterials)
		{
			remappingRoot.Clear();

			DisposeOfSerializedObjects();
			
			//Construct a dictionary between shaders and their materials
			shadersToMaterials = new Dictionary<Shader, List<SerializedObject>>();
			foreach (Material material in droppedMaterials)
			{
				if (!shadersToMaterials.TryGetValue(material.shader, out var list))
				{
					list = new List<SerializedObject>();
					shadersToMaterials.Add(material.shader, list);
				}

				list.Add(new SerializedObject(material));
			}

			foreach (var pair in shadersToMaterials)
			{
				Box shaderRoot = new Box();
				shaderRoot.AddToClassList("innerPadding");
				remappingRoot.Add(shaderRoot);

				Shader shader = pair.Key;
				ObjectField shaderField = new ObjectField {objectType = typeof(Shader), value = shader};
				shaderField.SetEnabled(false);
				shaderRoot.Add(shaderField);

				//RemapType, to a set of keys referring to values that could be remapped.
				var remapsFrom = new Dictionary<RemapType, HashSet<string>>();
				var remapsTo = new Dictionary<RemapType, HashSet<string>>();

				List<SerializedObject> materials = pair.Value;
				foreach (SerializedObject materialSO in materials)
				{
					SerializedProperty savedProperties = materialSO.FindProperty("m_SavedProperties");

					//Textures -------------------------------------------------------------------------
					SerializedProperty texEnvsArray = savedProperties.FindPropertyRelative("m_TexEnvs");
					var textureKeys = new HashSet<string>();
					remapsTo.Add(RemapType.Texture, textureKeys);
					var textureKeysFrom = new HashSet<string>();
					remapsFrom.Add(RemapType.Texture, textureKeysFrom);
					for (int i = 0; i < texEnvsArray.arraySize; i++)
					{
						SerializedProperty texEnv = texEnvsArray.GetArrayElementAtIndex(i);
						SerializedProperty key = texEnv.FindPropertyRelative("first");
						SerializedProperty texture = texEnv.FindPropertyRelative("second.m_Texture");

						textureKeys.Add(key.stringValue);
						//Only add to the "from", if there is a key with a meaningful value in it.
						if (texture.objectReferenceValue != null)
							textureKeysFrom.Add(key.stringValue);
					}
					//-----------------------------------------------------------------------------------

					//Floats ----------------------------------------------------------------------------
					SerializedProperty floatsArray = savedProperties.FindPropertyRelative("m_Floats");
					//floats have no way of telling if they're not set, so they can use the same set
					var floatKeys = new HashSet<string>();
					remapsTo.Add(RemapType.Float, floatKeys);
					remapsFrom.Add(RemapType.Float, floatKeys);
					for (int i = 0; i < floatsArray.arraySize; i++)
					{
						SerializedProperty @float = floatsArray.GetArrayElementAtIndex(i);
						SerializedProperty key = @float.FindPropertyRelative("first");
						floatKeys.Add(key.stringValue);
					}
					//-----------------------------------------------------------------------------------

					//Vectors and Colors ----------------------------------------------------------------
					SerializedProperty colorsArray = savedProperties.FindPropertyRelative("m_Colors");
					//colors have no way of telling if they're not set, so they can use the same set
					var vectorKeys = new HashSet<string>();
					remapsTo.Add(RemapType.VectorOrColor, vectorKeys);
					remapsFrom.Add(RemapType.VectorOrColor, vectorKeys);
					for (int i = 0; i < colorsArray.arraySize; i++)
					{
						SerializedProperty vector = colorsArray.GetArrayElementAtIndex(i);
						SerializedProperty key = vector.FindPropertyRelative("first");
						vectorKeys.Add(key.stringValue);
					}

					//-----------------------------------------------------------------------------------
				}

				//CONTROLS
				var remapTypeField = new EnumField("Remap Type", RemapType.None)
				{
					name = remapTypeName
				};
				remapTypeField.RegisterCallback<ChangeEvent<Enum>>(SelectedRemapType);
				shaderRoot.Add(remapTypeField);

				PopupField<string> popupFromField = new PopupField<string>(fromKeyLabel, noneList, 0)
				{
					name = fromKeyPopupName
				};
				popupFromField.SetEnabled(false);
				shaderRoot.Add(popupFromField);

				PopupField<string> popupToField = new PopupField<string>(toKeyLabel, noneList, 0)
				{
					name = toKeyPopupName
				};
				popupToField.SetEnabled(false);
				shaderRoot.Add(popupToField);

				Button remapButton = new Button {text = "Remap", name = remapButtonName};
				remapButton.SetEnabled(false);
				shaderRoot.Add(remapButton);


				void SelectedRemapType(ChangeEvent<Enum> remapEvt)
				{
					if (remapEvt == null)
						throw new ArgumentNullException(nameof(remapEvt));

					RemapType value = (RemapType) remapEvt.newValue;
					if (value == RemapType.None)
					{
						GetButton().SetEnabled(false);
						GetRemapTo().SetEnabled(false);
						GetRemapFrom().SetEnabled(false);
						return;
					}

					PopupField<string> child = GetRemapFrom();
					int index = shaderRoot.IndexOf(child);
					child.RemoveFromHierarchy();

					List<string> choicesFrom = remapsFrom[value].ToList();
					choicesFrom.Insert(0, none);
					popupFromField = new PopupField<string>(fromKeyLabel, choicesFrom, none)
					{
						name = fromKeyPopupName
					};
					shaderRoot.Insert(index, popupFromField);
					popupFromField.RegisterCallback<ChangeEvent<string>>(SelectedRemapFrom);
				}

				void SelectedRemapFrom(ChangeEvent<string> popupFromEvt)
				{
					string remapFromKey = popupFromEvt.newValue;
					if (remapFromKey == none)
					{
						GetButton().SetEnabled(false);
						GetRemapTo().SetEnabled(false);
						return;
					}

					PopupField<string> child = GetRemapTo();
					int index = shaderRoot.IndexOf(child);
					child.RemoveFromHierarchy();

					List<string> choicesTo = remapsTo[(RemapType) remapTypeField.value].ToList();
					choicesTo.Insert(0, none);
					popupToField = new PopupField<string>(toKeyLabel, choicesTo, none)
					{
						name = toKeyPopupName
					};
					shaderRoot.Insert(index, popupToField);
					popupToField.RegisterCallback<ChangeEvent<string>, (PopupField<string>, string)>(SelectedRemapTo, (popupToField, remapFromKey));
				}

				void SelectedRemapTo(ChangeEvent<string> popupToEvt, (PopupField<string> popupToField, string remapFromKey) args)
				{
					string remapToKey = popupToEvt.newValue;
					VisualElement background = args.popupToField.Q(null, PopupField<string>.inputUssClassName);
					if (remapToKey == none)
					{
						GetButton().SetEnabled(false);
						background.style.unityBackgroundImageTintColor = default;
						return;
					}

					if (remapToKey == args.remapFromKey)
					{
						background.style.unityBackgroundImageTintColor = new Color(1f, 0.46f, 0.51f);
						return;
					}

					background.style.unityBackgroundImageTintColor = default;

					var button = GetButton();
					button.SetEnabled(true);
					button.RemoveManipulator(button.clickable);
					button.clickable = new Clickable(() => PerformRemap(shader, (RemapType) GetRemapType().value, args.remapFromKey, remapToKey));
					button.AddManipulator(button.clickable);
				}

				Button GetButton() => shaderRoot.Q<Button>(remapButtonName);
				EnumField GetRemapType() => shaderRoot.Q<EnumField>(remapTypeName);
				PopupField<string> GetRemapFrom() => shaderRoot.Q<PopupField<string>>(fromKeyPopupName);
				PopupField<string> GetRemapTo() => shaderRoot.Q<PopupField<string>>(toKeyPopupName);
			}
		}

		void PerformRemap(Shader shader, RemapType remapType, string fromKey, string toKey)
		{
			if (remapType == RemapType.None)
			{
				Debug.LogError($"This shouldn't happen. {RemapType.None} has been provided to Perform Remap");
				return;
			}

			EditorUtility.DisplayProgressBar("Remapping", "Remapping materials", 0);
			StringBuilder log = new StringBuilder();
			try
			{

				string[] guids = AssetDatabase.FindAssets($"t:{nameof(Material)}");
				
				for (var index = 0; index < guids.Length; index++)
				{
					if (index % 5 == 0)
					{
						if (EditorUtility.DisplayCancelableProgressBar("Remapping", "Remapping materials", index / (float) (guids.Length - 1)))
						{
							EditorUtility.ClearProgressBar();
							return;
						}
					}

					string guid = guids[index];
					string path = AssetDatabase.GUIDToAssetPath(guid);
					var material = AssetDatabase.LoadAssetAtPath<Material>(path);
					try
					{

						if (material.shader != shader) continue;
						using (SerializedObject materialSO = new SerializedObject(material))
						{
							SerializedProperty savedProperties = materialSO.FindProperty("m_SavedProperties");
							SerializedProperty source;
							switch (remapType)
							{
								case RemapType.Texture:
									source = savedProperties.FindPropertyRelative("m_TexEnvs");
									break;
								case RemapType.Float:
									source = savedProperties.FindPropertyRelative("m_Floats");
									break;
								case RemapType.VectorOrColor:
									source = savedProperties.FindPropertyRelative("m_Colors");
									break;
								default:
									throw new ArgumentOutOfRangeException(nameof(remapType), remapType, null);
							}

							//Collect the source and destination remappings
							SerializedProperty from = null;
							int fromIndex = -1;
							int toIndex = -1;
							for (int i = 0; i < source.arraySize; i++)
							{
								SerializedProperty value = source.GetArrayElementAtIndex(i);
								SerializedProperty key = value.FindPropertyRelative("first");
								if (key.stringValue == fromKey)
								{
									from = value;
									fromIndex = i;
									if (toIndex != -1)
										break;
									continue;
								}

								if (key.stringValue == toKey)
								{
									toIndex = i;
									if (from != null)
										break;
								}
							}

							if (from == null)
							{
								log.AppendLine($"Failed to modify {materialSO.targetObject}. It did not contain the From property."); //<color=#aa4400ff>
								continue;
							}

							#if !OVERWRITE_SOURCE
						//TODO this has not been tested, it almost certainly needs to do a copy
						source.InsertArrayElementAtIndex(fromIndex);
						from = source.GetArrayElementAtIndex(fromIndex);
							#endif

							if (remapType == RemapType.Texture)
							{
								SerializedProperty texProp = from.FindPropertyRelative("second.m_Texture");
								if (texProp != null)
								{
									if (texProp.objectReferenceValue == null)
									{
										log.AppendLine($"{materialSO.targetObject} was skipped as it did not have a texture set in the From property."); //<color=#aa4400ff>
										continue;
									}
								}
							}

							//Re-assign from's key to be the destination key.
							from.FindPropertyRelative("first").stringValue = toKey;

							if (toIndex != -1)
							{
								//Delete pre-existing "to"
								source.DeleteArrayElementAtIndex(toIndex);
//							source.DeleteArrayElementAtIndex(toIndex);
							}

							materialSO.ApplyModifiedProperties();


							log.AppendLine($"Modified {materialSO.targetObject} successfully."); //<color=#22aa22ff>
						}
					}
					catch (Exception e)
					{
						log.AppendLine($"\"{path}\" was skipped as an exception occurred."); //<color=#aa4400ff>
						Debug.LogWarning("Exception was encountered, remap will continue.");
						Debug.LogException(e);
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			Debug.LogWarning(log.ToString());
		}
	}
}