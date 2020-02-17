using UnityEditor;
using UnityEngine;

namespace Vertx.Editor
{
	internal static class NUtilitiesPreferences
	{
		public const string PreferencesPath = "Preferences/nUtilities";
		private const string autoUpdatePackagesKey = "VERTX_AutoUpdatePackages";
		public static bool AutoUpdatePackages;
		private static readonly GUIContent packageUpdaterHeader = new GUIContent("Package Updater");
		private static readonly GUIContent autoUpdateLabel = new GUIContent("Auto Update", "Regularly checks for updates in Package Updaters.");

		static NUtilitiesPreferences() => AutoUpdatePackages = EditorPrefs.GetBool(autoUpdatePackagesKey, false);

		[SettingsProvider]
		public static SettingsProvider GetPreferences() =>
			new SettingsProvider(PreferencesPath, SettingsScope.User)
			{
				guiHandler = searchContext =>
				{
					GUILayout.Label(packageUpdaterHeader, EditorStyles.boldLabel);
					using (var cCS = new EditorGUI.ChangeCheckScope())
					{
						AutoUpdatePackages = EditorGUILayout.Toggle(autoUpdateLabel, AutoUpdatePackages);
						if (cCS.changed)
							EditorPrefs.SetBool(autoUpdatePackagesKey, AutoUpdatePackages);
					}
				}
			};
	}
}