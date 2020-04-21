#if NEWTONSOFT_JSON
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Vertx.Editor.Extensions;
using static Vertx.Editor.PackageUpdater;
using HelpBox = Vertx.Controls.HelpBox;

namespace Vertx.Editor
{
	/*
	 * At some point it may come to pass that this:
	 * https://forum.unity.com/threads/add-an-option-to-auto-update-packages.730628/#post-4931882
	 * Should be integrated additionally.
	 * By the looks of it now, I'll keep it separate.
	 */

	[CustomEditor(typeof(PackageUpdater))]
	public class PackageUpdaterInspector : UnityEditor.Editor
	{
		private SerializedProperty updatingPackages;

		private VisualTreeAsset packageItemUXML;
		private VisualElement root, packageRoot, addRoot, addContainer;
		private Button addButton, updateButton;
		private ToolbarSearchField search;
		private ListView listView;
		private const string hiddenStyle = "hidden";

		private string[] packagesInProject;
		private List<string> untrackedPackages;

		private void OnEnable()
		{
			updatingPackages = serializedObject.FindProperty(updatingPackagesProp);

			packagesInProject = GetPackagesInProject();
		}

		void AddHelpBox() => packageRoot.Add(new HelpBox("There are currently no packages auto-updating", HelpBox.MessageType.Info));
		void RemoveHelpBox() => packageRoot.Q<HelpBox>().RemoveFromHierarchy();

		public override VisualElement CreateInspectorGUI()
		{
			(StyleSheet styleSheet, VisualTreeAsset uxml) = StyleExtensions.GetStyleSheetAndUXML("PackageUpdater");
			packageItemUXML = StyleExtensions.GetUXML("PackageItem");

			root = new VisualElement();
			root.styleSheets.Add(styleSheet);
			root.styleSheets.Add(StyleExtensions.GetStyleSheet("VertxShared"));
			uxml.CloneTree(root);

			packageRoot = root.Q("Packages Root");
			addRoot = root.Q("Add Root");
			addContainer = addRoot.Q("Add Container");
			search = addContainer.Q<ToolbarSearchField>("Search");
			search.RegisterCallback<ChangeEvent<string>>(Search);

			if (updatingPackages.arraySize == 0)
				AddHelpBox();
			else
			{
				for (int i = 0; i < updatingPackages.arraySize; i++)
					AddTrackedPackage(i);
			}

			//-----------------------------------------------------
			addButton = addRoot.Q<Button>("Add Button");
			addButton.SetEnabled(false);
			addButton.clickable.clicked += PopulateAdd;
			listView = addRoot.Q<ListView>("Add Contents");
			// List view configuration
			listView.makeItem = () =>
			{
				Label button = new Label();
				button.AddToClassList("addPackageItemButton");
				return button;
			};
			listView.bindItem = (element, i) =>
			{
				string packageName = (string) listView.itemsSource[i];
				((Label) element).text = packageName;
			};
			#if UNITY_2020_1_OR_NEWER
			listView.onItemsChosen += objects =>
			{
				int c = untrackedPackages.Count;
				foreach (var o in objects)
				{
					//Add package so it can be tracked by the Package Updater.
					string packageName = (string) o;
					int index = updatingPackages.arraySize++;
					updatingPackages.GetArrayElementAtIndex(index).FindPropertyRelative(ignoreProp).stringValue = null;
					AddTrackedPackage(index, packageName);
					serializedObject.ApplyModifiedProperties();
					listView.Clear();
					addContainer.AddToClassList(hiddenStyle);
					c--;
				}

				//Disable the add button if there will be no more packages.
				if (c <= 0)
					DisableAddButton();
				else
					EnableAddButton();
			};
			#else
			listView.onItemChosen += o =>
			{
				//Disable the add button if there will be no more packages.
				if (untrackedPackages.Count <= 1)
					DisableAddButton();
				else
					EnableAddButton();

				//Add package so it can be tracked by the Package Updater.
				string packageName = (string) o;
				int index = updatingPackages.arraySize++;
				AddTrackedPackage(index, packageName);
				serializedObject.ApplyModifiedProperties();
				listView.Clear();
				addContainer.AddToClassList(hiddenStyle);
			};
			#endif
			addContainer.AddToClassList(hiddenStyle);
			//-----------------------------------------------------

			updateButton = root.Q<Button>("Update Button");
			updateButton.SetEnabled(false);
			updateButton.clickable.clicked += DoUpdate;

			var helpBox = root.Q<HelpBox>("Auto Update Help Box");
			helpBox.Q<Label>(className: HelpBox.uSSLabelClassName).text
				= NUtilitiesPreferences.AutoUpdatePackages ? "Auto-Update is enabled." : "Auto-Update is disabled.";
			helpBox.RegisterCallback<MouseUpEvent>(evt => SettingsService.OpenUserPreferences(NUtilitiesPreferences.PreferencesPath));

			ValidateAddButton();
			ValidatePackages();

			return root;
		}

		static string[] GetPackagesInProject() =>
			AssetDatabase.FindAssets("t:PackageManifest", new[] {"Packages"})
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(path =>
				{
					JObject json = JObject.Parse(File.ReadAllText(path));
					string name = (string) json["name"];
					//string version = (string) json["version"];
					return name;
				}).ToArray();

		void DoUpdate()
		{
			((PackageUpdater) target).UpdateTrackedPackages();
			serializedObject.Update();
			for (int i = 0; i < updatingPackages.arraySize; i++)
				SetIgnores(updatingPackages.GetArrayElementAtIndex(i), packageRoot[i]);
		}

		void AddTrackedPackage(int index, string nameOverride = null)
		{
			SerializedProperty arrayElement = updatingPackages.GetArrayElementAtIndex(index);
			SerializedProperty name = arrayElement.FindPropertyRelative(nameProp);
			if (nameOverride != null)
				name.stringValue = nameOverride;
			packageItemUXML.CloneTree(packageRoot);
			int i = packageRoot.childCount - 1;
			VisualElement itemRoot = packageRoot[i];
			var label = itemRoot.Q<Label>(null, "packageTitle");
			label.text = name.stringValue;
			var button = itemRoot.Q<Button>("Remove Item");
			int iLocal = i;
			button.clickable.clicked += () =>
			{
				//Remove tracked package
				updatingPackages.DeleteArrayElementAtIndex(iLocal);
				serializedObject.ApplyModifiedProperties();
				itemRoot.RemoveFromHierarchy();
				if (packageRoot.childCount == 0)
					AddHelpBox();
				EnableAddButton();
			};

			SetIgnores(arrayElement, itemRoot);
		}

		void SetIgnores(SerializedProperty arrayElement, VisualElement itemRoot)
		{
			//Ignores
			SerializedProperty ignore = arrayElement.FindPropertyRelative(ignoreProp);

			VisualElement ignoresRoot = itemRoot.Q("Ignores");
			if (string.IsNullOrEmpty(ignore.stringValue))
			{
				ignoresRoot.AddToClassList(hiddenStyle);
			}
			else
			{
				var labelIgnore = itemRoot.Q<Label>(null, "ignoreTitle");
				labelIgnore.text = $"Ignoring {ignore.stringValue}";
				var buttonIgnore = itemRoot.Q<Button>("Remove Ignore");
				ignoresRoot.RemoveFromClassList(hiddenStyle);
				buttonIgnore.RemoveManipulator(buttonIgnore.clickable);
				buttonIgnore.clickable = new Clickable(() =>
				{
					ignore.stringValue = null;
					serializedObject.ApplyModifiedProperties();
					ignoresRoot.AddToClassList(hiddenStyle);
				});
				buttonIgnore.AddManipulator(buttonIgnore.clickable);
			}
		}

		private void Search(ChangeEvent<string> evt)
		{
			string newValue = evt.newValue;
			if (string.IsNullOrEmpty(newValue))
			{
				listView.itemsSource = untrackedPackages;
				return;
			}

			listView.itemsSource = untrackedPackages.Where(packageName => packageName.Contains(newValue)).ToList();
		}

		private void ValidatePackages()
		{
			for (int i = 0; i < updatingPackages.arraySize; i++)
			{
				SerializedProperty arrayElementAtIndex = updatingPackages.GetArrayElementAtIndex(i);
				SerializedProperty name = arrayElementAtIndex.FindPropertyRelative(nameProp);
				if (packagesInProject.Any(packageName => packageName.Equals(name.stringValue)))
					continue;
				packageRoot[i].Add(new HelpBox($"{name.stringValue} is no longer present in the project. Remove this value from the Updater.", HelpBox.MessageType.Error));
			}
		}

		#region AddButton Helpers

		private void PopulateAdd()
		{
			if (listView.contentContainer.childCount > 0)
			{
				listView.Clear();
				addContainer.AddToClassList(hiddenStyle);
				addButton.text = "Add";
				return;
			}

			untrackedPackages = ((PackageUpdater) target).CollectUnTrackedPackages(packagesInProject);

			if (untrackedPackages.Count > 0)
			{
				addContainer.RemoveFromClassList(hiddenStyle);
				listView.itemsSource = untrackedPackages;
				addButton.text = "Cancel";
				//If anyone can figure out how to make this work properly, I'd love to know. SelectAll doesn't seem to work either.
				search.Q<TextField>().Focus();
			}
			else
				DisableAddButton();
		}

		private void ValidateAddButton()
		{
			if (((PackageUpdater) target).CollectUnTrackedPackages(packagesInProject).Count > 0)
				EnableAddButton();
			else
				DisableAddButton();
		}

		private void EnableAddButton()
		{
			addButton.SetEnabled(true);
			updateButton.SetEnabled(true);
			addButton.text = "Add";
		}

		private void DisableAddButton()
		{
			addButton.SetEnabled(false);
			updateButton.SetEnabled(false);
			addButton.text = "No more Packages in Project";
		}

		#endregion

		private void OnDisable()
		{
			//Sort the package list.
			var packageUpdater = (PackageUpdater) target;
			if (packageUpdater == null) return;
			object updatingPackagesList = EditorUtils.GetObjectFromProperty(updatingPackages, out _, out FieldInfo fieldInfo);
			if (updatingPackagesList != null)
			{
				var packageInfos = new List<TrackedPackage>((TrackedPackage[]) updatingPackagesList);
				packageInfos.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
				fieldInfo.SetValue(packageUpdater, packageInfos.ToArray());
				EditorUtility.SetDirty(packageUpdater);
			}
		}
	}
}
#endif