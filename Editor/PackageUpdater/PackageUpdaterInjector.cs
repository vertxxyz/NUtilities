#if NEWTONSOFT_JSON
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Vertx.Editor.Extensions;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Vertx.Editor
{
	public class PackageUpdaterInjector : IPackageManagerExtension
	{
		private const string injectedUSSFileName = "PackageUpdaterInjected";

		[InitializeOnLoadMethod]
		private static void Register() => PackageManagerExtensions.RegisterExtension(new PackageUpdaterInjector());

		public VisualElement CreateExtensionUI() =>
			new PackageUIInjector(
				parent =>
				{
					do
					{
						parent = parent.parent;
					} while (parent.parent != null && parent.name != "detailsContainer");


					var toolbar = parent.Q<VisualElement>("toolbarContainer");
					var right = toolbar.Q<VisualElement>("rightItems");

					(StyleSheet styleSheet, VisualTreeAsset tree) = StyleExtensions.GetStyleSheetAndUXML("PackageUpdaterInjected");
					
					var root = right.Q<VisualElement>("Package Updater Root");
					if(root == null)
						tree.CloneTree(right);

					root = right.Q<VisualElement>("Package Updater Root");
					root.SendToBack();
					root.styleSheets.Add(styleSheet);
					button = right.Q<Button>();
					button.clickable.clicked += () =>
					{
						if (currentlySelectedPackage == null) return;

						var packageUpdater = PackageUpdater.Instance;
						if (packageUpdater == null)
						{
							packageUpdater = ScriptableObject.CreateInstance<PackageUpdater>();
							AssetDatabase.CreateAsset(packageUpdater, "Assets/Package Updater.asset");
						}

						if (packageUpdater.Contains(currentlySelectedPackage))
						{
							packageUpdater.RemovePackage(currentlySelectedPackage);
							button.text = addText;
						}
						else
						{
							packageUpdater.AddPackage(currentlySelectedPackage);
							button.text = removeText;
						}
					};
					
					#if UNITY_2019_3_OR_NEWER
					right.style.paddingRight = 25;
					#endif
				}
			);

		private static readonly string removeText = $"Remove from {nameof(PackageUpdater)}";
		private static readonly string addText = $"Add to {nameof(PackageUpdater)}";
		private static readonly string createAndAddText = $"Create {nameof(PackageUpdater)} and add";

		private static Button button;
		private static PackageInfo currentlySelectedPackage;

		private static PackageInfo CurrentlySelectedPackage
		{
			get => currentlySelectedPackage;
			set
			{
				currentlySelectedPackage = value;
				if(value == null) return;
				var packageUpdater = PackageUpdater.Instance;
				if (button == null) return;
				if (packageUpdater != null)
					button.text = packageUpdater.Contains(value) ? removeText : addText;
				else
					button.text = createAndAddText;
			}
		}

		public void OnPackageSelectionChange(PackageInfo packageInfo) => CurrentlySelectedPackage = packageInfo;

		public void OnPackageAddedOrUpdated(PackageInfo packageInfo)
		{
			//TODO optionally automate an addition to package updater.
		}

		public void OnPackageRemoved(PackageInfo packageInfo)
		{
			var packageUpdater = PackageUpdater.Instance;
			if (packageUpdater == null) return;
			if (packageUpdater.RemovePackage(packageInfo))
				Debug.Log($"{packageInfo.name} was also removed from {packageUpdater}.");
		}

		private class PackageUIInjector : VisualElement
		{
			public PackageUIInjector(EventCallback<VisualElement> addedToParent) => RegisterCallback<AttachToPanelEvent>(evt => addedToParent(parent));
		}
	}
}
#endif