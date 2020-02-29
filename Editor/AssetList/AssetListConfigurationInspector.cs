using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using Vertx.Extensions;
using static Vertx.Editor.AssetListUtility;
using Object = UnityEngine.Object;

namespace Vertx.Editor
{
	[CustomEditor(typeof(AssetListConfiguration))]
	public class AssetListConfigurationInspector : ScriptableObjectInspector
	{
		private Type type;
		private bool typeIsTextureOrSprite;
		private bool typeIsAsset = true;

		private SerializedProperty
			assetType,
			columns,
			typeString,
			nameDisplay,
			iconPropertyPath;

		private ReorderableList reorderableList;

		private readonly GUIContent
			iconLabel = new GUIContent("Icon"),
			findIconLabel = new GUIContent("Find Icon Property"),
			columnsLabel = new GUIContent("Columns"),
			addLabel = new GUIContent("Add Column"),
			cancelLabel = new GUIContent("Cancel"),
			referenceObjectLabel = new GUIContent("Reference Object", "This object is used to gather Serialized Properties for column creation."),
			searchlabel = new GUIContent("Serialized Property Search"),
			addArrayKeyLabel = new GUIContent("Add Key", "Find a Serialized Property to use as a key into the above array property."),
			addDrawingPropertyLabel = new GUIContent("Add Drawing Property", "Find a Serialized Property to draw if the key's query has passed.");

		[SerializeField]
		private AdvancedDropdownState propertyDropdownState;

		private PropertyDropdown propertyDropdown;

		[SerializeField]
		private AdvancedDropdownState iconPropertyDropdownState;

		private PropertyDropdown iconPropertyDropdown;

		[SerializeField]
		private Object referenceObject;

		private readonly GUILayoutOption[] singleHeight = {GUILayout.Height(EditorGUIUtility.singleLineHeight)};

		private RectOffset backgroundOffset;

		private struct PropertyData
		{
			public SerializedPropertyType Type;
			public bool IsArray;
			public bool IsInConfiguration;

			public PropertyData(AssetListConfiguration configuration, SerializedProperty property)
			{
				Type = property.propertyType;
				IsArray = property.isArray;
				IsInConfiguration = configuration.Columns.Any(c => c.PropertyPath.Equals(property.propertyPath));
			}
		}
		
		private readonly Dictionary<int, float> heightOverrideLookup = new Dictionary<int, float>();
		
		protected override void OnEnable()
		{
			base.OnEnable();
			assetType = serializedObject.FindProperty("assetType");
			columns = serializedObject.FindProperty("columns");
			typeString = serializedObject.FindProperty("typeString");
			nameDisplay = serializedObject.FindProperty("nameDisplay");
			iconPropertyPath = serializedObject.FindProperty("iconPropertyPath");

			type = Type.GetType(typeString.stringValue);

			if (type == null) return;

			typeIsTextureOrSprite = typeof(Texture).IsAssignableFrom(type) || typeof(Sprite).IsAssignableFrom(type);
			typeIsAsset = !type.IsSubclassOf(typeof(Component));

			backgroundOffset = new RectOffset(20, 6, 2, 0);
			reorderableList = new ReorderableList(serializedObject, columns)
			{
				drawElementCallback = (rect, index, active, focused) =>
				{
					float min = rect.y;
					if (index % 2 == 0)
						EditorGUI.DrawRect(backgroundOffset.Add(rect), new Color(0f, 0f, 0f, 0.075f));
					SerializedProperty column = columns.GetArrayElementAtIndex(index);
					SerializedProperty propertyPath = column.FindPropertyRelative("PropertyPath");
					SerializedProperty title = column.FindPropertyRelative("Title");
					rect.height = EditorGUIUtility.singleLineHeight;
					EditorGUI.PropertyField(rect, title);
					rect.NextGUIRect();
					using (new EditorGUI.DisabledScope(true))
						EditorGUI.PropertyField(rect, propertyPath);

					var propertyType = (SerializedPropertyType) column.FindPropertyRelative("PropertyType").intValue;

					string propertyName = null;
					switch (propertyType)
					{
						case SerializedPropertyType.Generic:
							/*
							bool IsArray;
							ArrayIndexing ArrayIndexing;
							string ArrayPropertyKey;
							string ArrayQuery;
							int ArrayIndex;
							string ArrayPropertyPath;
							*/
							bool isArray = column.FindPropertyRelative("IsArray").boolValue;
							if (isArray)
							{
								SerializedProperty indexing = column.FindPropertyRelative("ArrayIndexing");
								rect.NextGUIRect();
								EditorGUI.PropertyField(rect, indexing);
								switch ((ArrayIndexing)indexing.intValue)
								{
									case ArrayIndexing.First:
										break;
									case ArrayIndexing.ByKey:
										//The property to look for as a key. This is associated with the query.
										rect.NextGUIRect();
										SerializedProperty key = column.FindPropertyRelative("ArrayPropertyKey");
										using(new EditorGUI.DisabledScope(true))
											EditorGUI.PropertyField(rect, key);
										rect.NextGUIRect();
										if (GUI.Button(rect, addArrayKeyLabel))
											DisplayArrayKeyPropertyDropdown(rect, $"{propertyPath.stringValue}.Array.data[0]", column);

										//The query into the array. This is associated with the array property key.
										if (!string.IsNullOrEmpty(key.stringValue))
										{
											rect.NextGUIRect();
											SerializedProperty arrayQuery = column.FindPropertyRelative("ArrayQuery");
											EditorGUI.PropertyField(rect, arrayQuery);

											//The property to draw if the query has passed.
											if (!string.IsNullOrEmpty(arrayQuery.stringValue))
											{
												rect.NextGUIRect();
												using(new EditorGUI.DisabledScope(true))
													EditorGUI.PropertyField(rect, column.FindPropertyRelative("ArrayPropertyPath"));
												rect.NextGUIRect();
												if (GUI.Button(rect, addDrawingPropertyLabel))
                                                	DisplayArrayDrawingPropertyDropdown(rect, $"{propertyPath.stringValue}.Array.data[0]", column);
											}
										}
										
										break;
									case ArrayIndexing.ByIndex:
										rect.NextGUIRect();
										EditorGUI.PropertyField(rect, column.FindPropertyRelative("ArrayIndex"));
										break;
									default:
										throw new ArgumentOutOfRangeException();
								}
								
								if (heightOverrideLookup.ContainsKey(index))
									heightOverrideLookup[index] = rect.yMax - min;
								else
									heightOverrideLookup.Add(index, rect.yMax - min);
							}
							break;
						case SerializedPropertyType.Integer:
						case SerializedPropertyType.Float:
							propertyName = "NumericalDisplay";
							break;
						case SerializedPropertyType.Boolean:
							break;
						case SerializedPropertyType.String:
							break;
						case SerializedPropertyType.Color:
							propertyName = "ColorDisplay";
							break;
						case SerializedPropertyType.ObjectReference:
							break;
						case SerializedPropertyType.LayerMask:
						case SerializedPropertyType.Enum:
							propertyName = "EnumDisplay";
							break;
						case SerializedPropertyType.Vector2:
							break;
						case SerializedPropertyType.Vector3:
							break;
						case SerializedPropertyType.Vector4:
							break;
						case SerializedPropertyType.Rect:
							break;
						case SerializedPropertyType.ArraySize:
							break;
						case SerializedPropertyType.Character:
							break;
						case SerializedPropertyType.AnimationCurve:
							break;
						case SerializedPropertyType.Bounds:
							break;
						case SerializedPropertyType.Gradient:
							break;
						case SerializedPropertyType.Quaternion:
							break;
						case SerializedPropertyType.ExposedReference:
							break;
						case SerializedPropertyType.FixedBufferSize:
							break;
						case SerializedPropertyType.Vector2Int:
							break;
						case SerializedPropertyType.Vector3Int:
							break;
						case SerializedPropertyType.RectInt:
							break;
						case SerializedPropertyType.BoundsInt:
							break;
						case SerializedPropertyType.ManagedReference:
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					if (propertyName != null)
					{
						rect.NextGUIRect();
						EditorGUI.PropertyField(rect, column.FindPropertyRelative(propertyName));
					}
				},
				headerHeight = 0,
				elementHeightCallback = index =>
				{
					if (heightOverrideLookup.TryGetValue(index, out float height))
						return height;
					return EditorGUIExtensions.HeightWithSpacing * 3;
				},
				onReorderCallback = list => heightOverrideLookup.Clear(),
				displayAdd = false,
				onRemoveCallback = list =>
				{
					ReorderableList.defaultBehaviours.DoRemoveButton(list);
					//Refresh property dropdown (this currently is only done to refresh the "enabled" state of the properties)
					CreatePropertyDropdown();
					heightOverrideLookup.Clear();
				}
			};

			referenceObject = LoadAssetByTypeName(type, type.IsSubclassOf(typeof(Component)), (AssetType) assetType.intValue);
			if (referenceObject != null)
				CreatePropertyDropdown();
		}

		public override void OnInspectorGUI()
		{
			serializedObject.UpdateIfRequiredOrScript();

			if (type == null)
			{
				EditorGUILayout.HelpBox("The type associated with this Asset List could not be found.", MessageType.Error);
				EditorGUILayout.PropertyField(typeString);
				serializedObject.ApplyModifiedProperties();
				return;
			}

			using (new EditorGUI.DisabledScope(typeIsAsset))
			{
				//Force assets to only display as InAssets.
				if (typeIsAsset && assetType.intValue != 0)
					assetType.intValue = 0;
				EditorGUILayout.PropertyField(assetType);
			}

			using (var cCS = new EditorGUI.ChangeCheckScope())
			{
				referenceObject = EditorGUILayout.ObjectField(referenceObjectLabel, referenceObject, type, true, singleHeight);
				if (cCS.changed)
				{
					if (referenceObject != null)
						CreatePropertyDropdown();
				}
			}

			GUILayout.Space(8);

			using (new EditorGUIExtensions.ContainerScope(columnsLabel, -2))
			using (new EditorGUIExtensions.OutlineScope(false, false))
			{
				GUILayout.Label(iconLabel, EditorStyles.miniLabel);

				if (typeIsTextureOrSprite)
					EditorGUILayout.HelpBox("Type inherits from Texture or Sprite. Icon is automated.", MessageType.Info);
				else
				{
					if (!string.IsNullOrEmpty(iconPropertyPath.stringValue))
						EditorGUILayout.PropertyField(iconPropertyPath, GUIContent.none);
					if (ValidateReferenceObjectWithHelpWarning())
					{
						Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
						rect.xMin += EditorGUI.indentLevel * 15;
						if (GUI.Button(rect, findIconLabel))
						{
							CreateIconPropertyDropdown();
							iconPropertyDropdown?.Show(rect);
						}
					}
				}

				using (new EditorGUI.DisabledScope(true))
				{
					EditorGUILayout.TextField("Title", "Name");
					EditorGUILayout.TextField("Property Path", "m_Name");
				}

				EditorGUILayout.PropertyField(nameDisplay);
			}

			reorderableList.DoLayoutList();

			using (new EditorGUIExtensions.OutlineScope())
			{
				GUILayout.Label(searchlabel, EditorStyles.boldLabel);
				if (ValidateReferenceObjectWithHelpWarning())
				{
					Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
					if (GUI.Button(rect, addLabel))
						propertyDropdown.Show(rect);
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		bool ValidateReferenceObjectWithHelpWarning()
		{
			if (referenceObject != null)
				return true;
			EditorGUILayout.HelpBox("A reference Object is required to search for Serialized Properties.", MessageType.Warning);
			return false;
		}

		void CreatePropertyDropdown()
		{
			if (referenceObject == null)
			{
				Debug.LogError("Reference Object is null. Lookup cannot be created");
				return;
			}

			HashSet<string> propertyPaths = new HashSet<string>();
			Dictionary<string, PropertyData> propertyLookup = new Dictionary<string, PropertyData>();
			var iterator = new ScriptableObjectIterator(referenceObject);
			var configuration = (AssetListConfiguration) target;
			foreach (SerializedProperty prop in iterator)
			{
				if (prop.propertyType == SerializedPropertyType.Generic)
				{
					if (prop.isArray)
					{
						propertyPaths.Add(prop.propertyPath);
						propertyLookup.Add(prop.propertyPath, new PropertyData (configuration, prop));

						if (!prop.NextVisible(false))
							break;
						iterator.PauseIteratorOnce = true;
					}

					//We can't display generic fields anyway.
					continue;
				}

				propertyPaths.Add(prop.propertyPath);
				propertyLookup.Add(prop.propertyPath, new PropertyData (configuration, prop));
			}

			if (propertyDropdownState == null)
				propertyDropdownState = new AdvancedDropdownState();

			propertyDropdown = new PropertyDropdown(propertyDropdownState, propPath =>
			{
				SerializedProperty column = columns.GetArrayElementAtIndex(columns.arraySize++);
				column.FindPropertyRelative("PropertyPath").stringValue = propPath;
				column.FindPropertyRelative("Title").stringValue = ObjectNames.NicifyVariableName(propPath);
				column.FindPropertyRelative("PropertyType").intValue = (int) propertyLookup[propPath].Type;
				column.FindPropertyRelative("IsArray").boolValue = propertyLookup[propPath].IsArray;
					serializedObject.ApplyModifiedProperties();
			}, propertyPaths, propertyLookup);
		}

		void CreateIconPropertyDropdown()
		{
			if (referenceObject == null)
			{
				Debug.LogError("Reference Object is null. Lookup cannot be created");
				return;
			}

			HashSet<string> iconPropertyPaths = new HashSet<string>();
			var iterator = new ScriptableObjectIterator(referenceObject);
			Type texType = typeof(Texture);
			Type spriteType = typeof(Sprite);

			List<string> typeStrings = new List<string>
			{
				$"PPtr<{nameof(Texture)}>",
				$"PPtr<{nameof(Sprite)}>"
			};
			typeStrings.AddRange(
				TypeCache.GetTypesDerivedFrom(texType)
					.Select(type1 => $"PPtr<{type1.Name.Replace("UnityEngine.", string.Empty)}>")
			);
			typeStrings.AddRange(
				TypeCache.GetTypesDerivedFrom(spriteType)
					.Select(type1 => $"PPtr<{type1.Name.Replace("UnityEngine.", string.Empty)}>")
			);

			foreach (SerializedProperty prop in iterator)
			{
				if (prop.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				if (prop.objectReferenceValue != null)
				{
					Type propType = prop.objectReferenceValue.GetType();
					if (texType.IsAssignableFrom(propType) || spriteType.IsAssignableFrom(propType))
					{
						iconPropertyPaths.Add(prop.propertyPath);
						continue;
					}
				}

				if (typeStrings.Contains(prop.type))
					iconPropertyPaths.Add(prop.propertyPath);
			}

			iconPropertyDropdown = null;

			switch (iconPropertyPaths.Count)
			{
				case 0:
					Debug.LogError("No appropriate property paths found.");
					return;
				case 1:
					Debug.Log("Only one appropriate property path found. It has been automatically assigned.");
					AssignPropPath(iconPropertyPaths.First());
					return;
			}

			if (iconPropertyDropdownState == null)
				iconPropertyDropdownState = new AdvancedDropdownState();

			iconPropertyDropdown = new PropertyDropdown(iconPropertyDropdownState, AssignPropPath, iconPropertyPaths, null);

			void AssignPropPath(string propPath)
			{
				iconPropertyPath.stringValue = propPath;
				serializedObject.ApplyModifiedProperties();
			}
		}

		private void DisplayArrayKeyPropertyDropdown(Rect rect, string propertyPath, SerializedProperty column)
		{
			HashSet<string> propertyPaths = new HashSet<string>();

			var iterator = new ScriptableObjectPropertyIterator(referenceObject, propertyPath);
			if (!iterator.IsValid())
			{
				Debug.LogError($"The current reference object {referenceObject} does not contain an array of this type with values in it. Try another reference object.");
				return;
			}
			
			foreach (SerializedProperty property in iterator)
			{
				if(!IsValidPropertyKeyType(property.propertyType)) continue;
				propertyPaths.Add(property.propertyPath.Substring(propertyPath.Length + 1));// + 1 to skip the '.'
			}

			PropertyDropdown dropdown = new PropertyDropdown(new AdvancedDropdownState(), s =>
			{
				column.FindPropertyRelative("ArrayPropertyKey").stringValue = s;
				column.serializedObject.ApplyModifiedProperties();
			}, propertyPaths, null);
			dropdown.Show(rect);
		}
		
		private void DisplayArrayDrawingPropertyDropdown(Rect rect, string propertyPath, SerializedProperty column)
		{
			HashSet<string> propertyPaths = new HashSet<string>();

			var iterator = new ScriptableObjectPropertyIterator(referenceObject, propertyPath);
			if (!iterator.IsValid())
			{
				Debug.LogError($"The current reference object {referenceObject} does not contain an array of this type with values in it. Try another reference object.");
				return;
			}
			
			foreach (SerializedProperty property in iterator)
			{
				if(property.propertyType == SerializedPropertyType.Generic) continue;
				propertyPaths.Add(property.propertyPath.Substring(propertyPath.Length + 1));// + 1 to skip the '.'
			}

			PropertyDropdown dropdown = new PropertyDropdown(new AdvancedDropdownState(), s =>
			{
				column.FindPropertyRelative("ArrayPropertyPath").stringValue = s;
				column.serializedObject.ApplyModifiedProperties();
			}, propertyPaths, null);
			dropdown.Show(rect);
		}

		private class ScriptableObjectIterator : IEnumerable<SerializedProperty>
		{
			private readonly SerializedObject serializedObject;
			public bool PauseIteratorOnce;

			public ScriptableObjectIterator(Object referenceObject)
			{
				serializedObject = new SerializedObject(referenceObject);
				typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(serializedObject, 1);
			}

			public IEnumerator<SerializedProperty> GetEnumerator()
			{
				SerializedProperty prop = serializedObject.GetIterator();
				while (PauseIteratorOnce || prop.NextVisible(true))
				{
					PauseIteratorOnce = false;
					yield return prop;
				}

				serializedObject.Dispose();
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
		
		private class ScriptableObjectPropertyIterator : IEnumerable<SerializedProperty>
		{
			private readonly SerializedObject serializedObject;
			private readonly SerializedProperty prop;

			public ScriptableObjectPropertyIterator(Object referenceObject, string parentPropertyPath)
			{
				serializedObject = new SerializedObject(referenceObject);
				typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(serializedObject, 1);
				prop = serializedObject.FindProperty(parentPropertyPath);
			}

			public bool IsValid() => prop != null;

			public IEnumerator<SerializedProperty> GetEnumerator()
			{
				SerializedProperty end = prop.GetEndProperty();
				while (prop.NextVisible(true) && !SerializedProperty.EqualContents(prop, end))
					yield return prop;
				serializedObject.Dispose();
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		private class PropertyDropdown : AdvancedDropdown
		{
			private readonly Action<string> itemSelected;
			private readonly HashSet<string> propertyPaths;
			private readonly Dictionary<string, PropertyData> propertyLookup;
			private readonly Dictionary<int, string> pathLookup = new Dictionary<int, string>();

			public PropertyDropdown(AdvancedDropdownState state, Action<string> itemSelected, HashSet<string> propertyPaths, Dictionary<string, PropertyData> propertyLookup) : base(state)
			{
				this.itemSelected = itemSelected;
				this.propertyPaths = propertyPaths;
				this.propertyLookup = propertyLookup;
				minimumSize = new Vector2(200, 300);
			}

			protected override void ItemSelected(AdvancedDropdownItem item)
			{
				if (!pathLookup.TryGetValue(item.id, out var propPath))
				{
					Debug.LogError($"{item.name} has no type lookup.");
					return;
				}

				itemSelected.Invoke(propPath);
			}

			private static string GetName(string path)
			{
				int indexOfSeparator = path.LastIndexOf('/');
				if (indexOfSeparator < 0 || indexOfSeparator == path.Length - 1)
					return path;
				return path.Substring(indexOfSeparator + 1);
			}

			protected override AdvancedDropdownItem BuildRoot()
			{
				pathLookup.Clear();
				var root = new AdvancedDropdownItem("Serialized Property Search");


				Dictionary<string, AdvancedDropdownItem> dropdownItemsDict = new Dictionary<string, AdvancedDropdownItem>();

				foreach (var propertyPath in propertyPaths)
				{
					var path = propertyPath;
					AdvancedDropdownItem dropdownItem = root;
					string name = GetName(path);

					//If there is a path attribute
					if (!string.IsNullOrEmpty(path))
					{
						AdvancedDropdownItem depthFirst = null;
						string menuPath = null;
						do
						{
							//Get the menu path and name---------
							int lastSeparator = path.LastIndexOf('/');
							if (lastSeparator < 0)
							{
								//If the menu path is no longer a path, add it to the root.
								if (string.IsNullOrEmpty(menuPath))
									menuPath = path;
								AdvancedDropdownItem baseDropDownItem = new AdvancedDropdownItem(
									ObjectNames.NicifyVariableName(menuPath)
								);
								if (propertyLookup?[propertyPath].IsInConfiguration ?? false)
									baseDropDownItem.enabled = false;
								if (depthFirst != null)
									baseDropDownItem.AddChild(depthFirst);
								else
								{
									pathLookup.Add(baseDropDownItem.id, path);
									baseDropDownItem.icon = GetIcon(path);
								}

								root.AddChild(baseDropDownItem);
								break;
							}

							menuPath = path.Substring(0, lastSeparator);
							name = ObjectNames.NicifyVariableName(path.Substring(lastSeparator + 1));
							//------------------------------------

							if (string.IsNullOrEmpty(menuPath))
							{
								Debug.LogError($"{path} was not a valid path for this method.");
								break;
							}

							AdvancedDropdownItem newDropDownItem = new AdvancedDropdownItem(name);
							if (propertyLookup?[propertyPath].IsInConfiguration ?? false)
							{
								newDropDownItem.enabled = false;
							}

							if (depthFirst != null)
								newDropDownItem.AddChild(depthFirst);
							else
							{
								pathLookup.Add(newDropDownItem.id, path);
								newDropDownItem.icon = GetIcon(path);
							}

							depthFirst = newDropDownItem;
							dropdownItemsDict.Add(path, newDropDownItem);
							path = menuPath;
						} while (!dropdownItemsDict.TryGetValue(menuPath, out dropdownItem));

						continue;
					}

					dropdownItem.AddChild(new AdvancedDropdownItem(name));
				}

				return root;

				Texture2D GetIcon(string p)
				{
					return EditorGUIUtility.FindTexture("cs Script Icon");

					/*Texture2D icon = EditorGUIUtility.ObjectContent(null, t).image as Texture2D;
					if(icon == null || icon.name.StartsWith("DefaultAsset"))
						icon = EditorGUIUtility.FindTexture("cs Script Icon");
					return icon;*/
				}
			}
		}
	}
}