using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Vertx.Editor
{
	internal readonly struct PropertyData
	{
		public readonly SerializedPropertyType Type;
		public readonly bool IsArray;
		public readonly bool IsInConfiguration;

		public PropertyData(AssetListConfiguration configuration, SerializedProperty property)
		{
			Type = property.propertyType;
			IsArray = property.isArray;
			IsInConfiguration = configuration.Columns != null &&
			                    configuration.Columns.Any(c => c.PropertyPath.Equals(property.propertyPath));
		}
	}

	internal class PropertyDropdown : AdvancedDropdown
	{
		private readonly Action<string> itemSelected;
		private readonly HashSet<string> propertyPaths;
		private readonly Dictionary<string, PropertyData> propertyLookup;
		private readonly Dictionary<int, string> pathLookup = new Dictionary<int, string>();

		public PropertyDropdown(AdvancedDropdownState state, Action<string> itemSelected, HashSet<string> propertyPaths,
			Dictionary<string, PropertyData> propertyLookup) : base(state)
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