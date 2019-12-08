using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Vertx.Extensions {
	public static class EditorGUIExtensions
	{
		#region Exponential Slider

		public static void ExponentialSlider(SerializedProperty property, float yMin, float yMax, params GUILayoutOption[] options) => 
			property.floatValue = ExponentialSlider(new GUIContent(property.displayName), property.floatValue, yMin, yMax, options);

		/// <summary>
		/// Exponential slider.
		/// </summary>
		/// <returns>The exponential value</returns>
		/// <param name="val">Value.</param>
		/// <param name="yMin">Ymin at x = 0.</param>
		/// <param name="yMax">Ymax at x = 1.</param>
		/// <param name="options"></param>
		public static float ExponentialSlider(float val, float yMin, float yMax, params GUILayoutOption[] options)
		{
			Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, options);
			return ExponentialSlider(controlRect, val, yMin, yMax);
		}

		public static float ExponentialSlider(GUIContent label, float val, float yMin, float yMax, params GUILayoutOption[] options)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PrefixLabel(label);
				val = ExponentialSlider(val, yMin, yMax, options);
			}
			return val;
		}

		public static float ExponentialSlider(Rect position, float val, float yMin, float yMax)
		{
			val = Mathf.Clamp(val, yMin, yMax);
			yMin -= 1;
			float x = Mathf.Log(val - yMin) / Mathf.Log(yMax - yMin);
			x = EditorGUI.Slider(position, x, 0, 1);
			float y = Mathf.Pow(yMax - yMin, x) + yMin;
			position.x = position.x + position.width - 50;
			position.width = 50;
			GUI.Box(position, GUIContent.none);

			GUI.SetNextControlName("vertxFloatField");
			y = Mathf.Clamp(EditorGUI.FloatField(position, y), yMin, yMax);
			if (position.Contains(Event.current.mousePosition))
				GUI.FocusControl("vertxFloatField");

			return y;
		}

		#endregion

		public static bool ButtonOverPreviousControl () {
			Rect r = GUILayoutUtility.GetLastRect ();
			return GUI.Button (r, GUIContent.none, GUIStyle.none);
		}
		
		#region Folders
		public static void ShowFolderContents(int folderInstanceId, bool revealAndFrameInFolderTree)
		{
			MethodInfo showContentsMethod =
				projectBrowserType.GetMethod("ShowFolderContents", BindingFlags.NonPublic | BindingFlags.Instance);
			EditorWindow browser = EditorWindow.GetWindow(projectBrowserType);
			if (browser != null)
				showContentsMethod.Invoke(browser, new object[] {folderInstanceId, revealAndFrameInFolderTree});
		}
	
		public static int GetMainAssetInstanceID(string path)
		{
			object idObject = typeof(AssetDatabase).GetMethod("GetMainAssetInstanceID", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, new object[] {path});
			if (idObject != null) return (int) idObject;
			return -1;
		}

		public static void ShowFolder(DefaultAsset o)
		{
			if (o == null)
				return;

			string path = AssetDatabase.GetAssetPath (o);
			if (Path.GetFileName(path).Contains("."))
				return; 	//DefaultAsset is a file.
			ShowFolderContents(
				GetMainAssetInstanceID(AssetDatabase.GUIDToAssetPath(AssetDatabase.AssetPathToGUID(path))), true
			);
			GetProjectBrowserWindow(true).Repaint();
		}
		#endregion

		#region Project Browser

		public static void SetProjectBrowserSearch(string search)
		{
			EditorWindow window = GetProjectBrowserWindow(true);
			projectBrowserSetSearch.Invoke(window, new object[]{search});
		}
		
		private static Type _projectBrowserType;
		private static Type projectBrowserType => _projectBrowserType ?? (_projectBrowserType =
			                                          Type.GetType("UnityEditor.ProjectBrowser,UnityEditor"));
		
		private static MethodInfo _projectBrowserSetSearch;
		private static MethodInfo projectBrowserSetSearch => projectBrowserType.GetMethod("SetSearch", new []{typeof(string)});
		
		
		public static EditorWindow GetProjectBrowserWindow(bool forceOpen = false)
		{
			EditorWindow projectBrowser = EditorWindow.GetWindow(projectBrowserType);
			if (projectBrowser != null)
				return projectBrowser;
			if(!forceOpen)
				return null;
			EditorApplication.ExecuteMenuItem ("Window/General/Project");
			return EditorWindow.GetWindow(projectBrowserType);
		}

		#endregion
	}
}