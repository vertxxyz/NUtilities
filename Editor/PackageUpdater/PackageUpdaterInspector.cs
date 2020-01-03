using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.UIElements;
using Vertx.Controls;
using Vertx.Extensions;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using static Vertx.Editor.PackageUpdater;

namespace Vertx.Editor
{
	[CustomEditor(typeof(PackageUpdater))]
	public class PackageUpdaterInspector : UnityEditor.Editor
	{
		ListRequest request;

		private SerializedProperty updatingPackages;

		private VisualTreeAsset packageItemUXML;
		private VisualElement root, packageRoot, addRoot;
		private Button addButton, updateButton;
		private ListView listView;


		private void OnEnable()
		{
			updatingPackages = serializedObject.FindProperty(updatingPackagesProp);

			request = Client.List(); // List packages installed for the Project
			EditorApplication.update += Progress;
		}

		void AddHelpBox() => packageRoot.Add(new HelpBox("There are currently no packages auto-updating", HelpBox.MessageType.Info));
		void RemoveHelpBox() => packageRoot.Q<HelpBox>().RemoveFromHierarchy();

		public override VisualElement CreateInspectorGUI()
		{
			PackageUpdater packageUpdater = (PackageUpdater) target;
			(StyleSheet styleSheet, VisualTreeAsset uxml) = StyleExtensions.GetStyleSheetAndUXML("PackageUpdater");
			packageItemUXML = StyleExtensions.GetUXML("PackageItem");

			root = new VisualElement();
			root.styleSheets.Add(styleSheet);
			root.styleSheets.Add(StyleExtensions.GetStyleSheet("VertxShared"));
			uxml.CloneTree(root);

			packageRoot = root.Q("Packages Root");
			addRoot = root.Q("Add Root");

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
				PackageInfo packageInfo = (PackageInfo) listView.itemsSource[i];
				((Label) element).text = packageInfo.name;
			};
			#if UNITY_2020_1_OR_NEWER
			listView.onItemsChosen += objects =>
			{
				int c = listView.itemsSource.Count;
				foreach (var o in objects)
				{
					//Add package so it can be tracked by the Package Updater.
					PackageInfo packageInfo = (PackageInfo) o;
					int index = updatingPackages.arraySize++;
					AddTrackedPackage(index, packageInfo.name);
					serializedObject.ApplyModifiedProperties();
					listView.Clear();
					listView.RemoveFromHierarchy();
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
				if (listView.itemsSource.Count <= 1)
					DisableAddButton();
				else
					EnableAddButton();

				//Add package so it can be tracked by the Package Updater.
				PackageInfo packageInfo = (PackageInfo) o;
				int index = updatingPackages.arraySize++;
				AddTrackedPackage(index, packageInfo.name);
				serializedObject.ApplyModifiedProperties();
				listView.Clear();
				listView.RemoveFromHierarchy();
			};
			#endif
			listView.RemoveFromHierarchy();
			//-----------------------------------------------------

			updateButton = root.Q<Button>("Update Button");
			updateButton.SetEnabled(false);

			void UpdateButtonFunctionality()
			{
				List<PackageInfo> trackedPackages = CollectTrackedPackages();
				foreach (PackageInfo trackedPackage in trackedPackages)
				{
					string newVersion = trackedPackage.versions.latestCompatible;
					string packageName = trackedPackage.name;
					string packageVersion = trackedPackage.version;

					if (string.IsNullOrEmpty(newVersion))
					{
						Debug.LogWarning($"{packageName} ({packageVersion}) has no latest version. It may be a package from git, this is not currently supported.");
						continue;
					}

					if (packageVersion.Equals(newVersion))
					{
						Debug.Log($"{packageName} is up to date. ({packageVersion}).");
						continue;
					}

					if (packageUpdater.IsVersionIgnored(packageName, newVersion))
					{
						Debug.Log($"{packageName} ({newVersion}) is currently being ignored by the package updater.\nCurrent version: {packageVersion}.");
						continue;
					}

					int selection = EditorUtility.DisplayDialogComplex("Package Updater", $"{trackedPackage.displayName} ({packageName}) can be updated.\n{packageVersion} to {newVersion}", $"Update to {newVersion}", "Ignore Once", "Skip Version");
					switch (selection)
					{
						case 0:
							// Update
							//TODO update package
							break;
						case 1:
							// Ignore Once
							Debug.Log($"{packageName} ({packageVersion}) has been ignored once.");
							break;
						case 2:
							// Skip Version
							packageUpdater.SetToSkipPackageVersion(packageName, newVersion);
							serializedObject.SetIsDifferentCacheDirty();
							serializedObject.Update();
							int index = packageUpdater.GetIndexOf(packageName);
							SetIgnores(updatingPackages.GetArrayElementAtIndex(index), packageRoot[index]);
							break;
						default:
							throw new NotImplementedException($"Return status: {selection}, from DisplayDialogComplex not supported.");
					}
				}
			}

			updateButton.clickable.clicked += UpdateButtonFunctionality;

			return root;
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
				ignoresRoot.style.display = DisplayStyle.None;
			}
			else
			{
				var labelIgnore = itemRoot.Q<Label>(null, "ignoreTitle");
				labelIgnore.text = $"Ignoring {ignore.stringValue}";
				var buttonIgnore = itemRoot.Q<Button>("Remove Ignore");
				ignoresRoot.style.display = DisplayStyle.Flex;
				buttonIgnore.clickable = new Clickable(() =>
				{
					ignore.stringValue = null;
					serializedObject.ApplyModifiedProperties();
					ignoresRoot.style.display = DisplayStyle.None;
				});
			}
		}

		private PackageCollection packageCollection;

		private List<PackageInfo> CollectUnTrackedPackages()
		{
			//Collect the names of all the packages we're currently tracking and updating.
			HashSet<string> currentlyUpdatingNames = new HashSet<string>();
			for (int i = 0; i < updatingPackages.arraySize; i++)
			{
				SerializedProperty arrayElementAtIndex = updatingPackages.GetArrayElementAtIndex(i);
				SerializedProperty name = arrayElementAtIndex.FindPropertyRelative(nameProp);
				currentlyUpdatingNames.Add(name.stringValue);
			}

			return packageCollection.Where(package => !currentlyUpdatingNames.Contains(package.name)).ToList();
		}

		private List<PackageInfo> CollectTrackedPackages()
		{
			//Collect the names of all the packages we're currently tracking and updating.
			HashSet<string> currentlyUpdatingNames = new HashSet<string>();
			for (int i = 0; i < updatingPackages.arraySize; i++)
			{
				SerializedProperty arrayElementAtIndex = updatingPackages.GetArrayElementAtIndex(i);
				SerializedProperty name = arrayElementAtIndex.FindPropertyRelative(nameProp);
				currentlyUpdatingNames.Add(name.stringValue);
			}

			return packageCollection.Where(package => currentlyUpdatingNames.Contains(package.name)).ToList();
		}

		#region AddButton Helpers

		private void PopulateAdd()
		{
			if (listView.contentContainer.childCount > 0)
			{
				listView.Clear();
				listView.RemoveFromHierarchy();
				addButton.text = "Add";
				return;
			}

			List<PackageInfo> packages = CollectUnTrackedPackages();

			if (packages.Count > 0)
			{
				addRoot.Add(listView);
				listView.itemsSource = packages;
				addButton.text = "Cancel";
			}
			else
				DisableAddButton();
		}

		private void ValidateAddButton()
		{
			if (CollectUnTrackedPackages().Count > 0)
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

		void Progress()
		{
			if (!request.IsCompleted)
				return;
			try
			{
				switch (request.Status)
				{
					case StatusCode.Success:
						packageCollection = request.Result;
						//Exit early in case of NRE (happens when recompiling with this open)
						if (addButton != null)
							ValidateAddButton();
						break;
					case StatusCode.InProgress:
						break;
					case StatusCode.Failure:
						Debug.Log(request.Error.message);
						break;
					default:
						throw new NotImplementedException($"Request status: {request.Status}, not supported.");
				}
			}
			finally
			{
				EditorApplication.update -= Progress;
			}
		}

		private void OnDisable()
		{
			EditorApplication.update -= Progress;

			//Sort the package list.
			var packageUpdater = (PackageUpdater) target;
			object updatingPackagesList = EditorUtils.GetObjectFromProperty(updatingPackages, out _, out FieldInfo fieldInfo);
			if (updatingPackagesList != null)
			{
				var packageInfos = new List<PackageUpdater.PackageInfo>((PackageUpdater.PackageInfo[]) updatingPackagesList);
				packageInfos.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
				fieldInfo.SetValue(packageUpdater, packageInfos.ToArray());
				EditorUtility.SetDirty(packageUpdater);
			}
		}
	}
}