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
			iconPropertyPath;

		private ReorderableList reorderableList;

		private readonly GUIContent
			iconLabel = new GUIContent("Icon"),
			findIconLabel = new GUIContent("Find Icon Property"),
			columnsLabel = new GUIContent("Columns"),
			addLabel = new GUIContent("Add Column"),
			cancelLabel = new GUIContent("Cancel"),
			referenceObjectLabel = new GUIContent("Reference Object", "This object is used to gather Serialized Properties for column creation."),
			searchlabel = new GUIContent("Serialized Property Search");

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

		protected override void OnEnable()
		{
			base.OnEnable();
			assetType = serializedObject.FindProperty("assetType");
			columns = serializedObject.FindProperty("columns");
			typeString = serializedObject.FindProperty("typeString");
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
							break;
						case SerializedPropertyType.ObjectReference:
							break;
						case SerializedPropertyType.LayerMask:
							break;
						case SerializedPropertyType.Enum:
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
				elementHeight = EditorGUIExtensions.HeightWithSpacing * 3,
				displayAdd = false
			};

			referenceObject = AssetListUtility.LoadAssetByTypeName(type, type.IsSubclassOf(typeof(Component)), (AssetType) assetType.intValue);
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
			Dictionary<string, SerializedPropertyType> typeLookup = new Dictionary<string, SerializedPropertyType>();
			var iterator = new ScriptableObjectIterator(referenceObject);
			foreach (SerializedProperty prop in iterator)
			{
				propertyPaths.Add(prop.propertyPath);
				typeLookup.Add(prop.propertyPath, prop.propertyType);
			}

			if (propertyDropdownState == null)
				propertyDropdownState = new AdvancedDropdownState();

			propertyDropdown = new PropertyDropdown(propertyDropdownState, propPath =>
			{
				SerializedProperty column = columns.GetArrayElementAtIndex(columns.arraySize++);
				column.FindPropertyRelative("PropertyPath").stringValue = propPath;
				column.FindPropertyRelative("Title").stringValue = ObjectNames.NicifyVariableName(propPath);
				column.FindPropertyRelative("PropertyType").intValue = (int) typeLookup[propPath];
				serializedObject.ApplyModifiedProperties();
			}, propertyPaths);
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

			iconPropertyDropdown = new PropertyDropdown(iconPropertyDropdownState, AssignPropPath, iconPropertyPaths);

			void AssignPropPath(string propPath)
			{
				iconPropertyPath.stringValue = propPath;
				serializedObject.ApplyModifiedProperties();
			}
		}

		private class ScriptableObjectIterator : IEnumerable<SerializedProperty>
		{
			private readonly SerializedObject serializedObject;

			public ScriptableObjectIterator(Object referenceObject) => serializedObject = new SerializedObject(referenceObject);

			public IEnumerator<SerializedProperty> GetEnumerator()
			{
				typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(serializedObject, 1);
				SerializedProperty prop = serializedObject.GetIterator();
				while (prop.NextVisible(true))
					yield return prop;
				serializedObject.Dispose();
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		private class PropertyDropdown : AdvancedDropdown
		{
			private readonly Action<string> itemSelected;
			private readonly HashSet<string> propertyPaths;
			private readonly Dictionary<int, string> pathLookup = new Dictionary<int, string>();

			public PropertyDropdown(AdvancedDropdownState state, Action<string> itemSelected, HashSet<string> propertyPaths) : base(state)
			{
				this.itemSelected = itemSelected;
				this.propertyPaths = propertyPaths;
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
								AdvancedDropdownItem baseDropDownItem = new AdvancedDropdownItem(ObjectNames.NicifyVariableName(menuPath));
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