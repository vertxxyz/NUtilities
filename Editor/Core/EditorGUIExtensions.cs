using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Vertx.Extensions
{
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

		public static bool ButtonOverPreviousControl()
		{
			Rect r = GUILayoutUtility.GetLastRect();
			return GUI.Button(r, GUIContent.none, GUIStyle.none);
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

			string path = AssetDatabase.GetAssetPath(o);
			if (Path.GetFileName(path).Contains("."))
				return; //DefaultAsset is a file.
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
			projectBrowserSetSearch.Invoke(window, new object[] {search});
		}

		private static Type _projectBrowserType;

		private static Type projectBrowserType => _projectBrowserType ?? (_projectBrowserType =
			Type.GetType("UnityEditor.ProjectBrowser,UnityEditor"));

		private static MethodInfo _projectBrowserSetSearch;
		private static MethodInfo projectBrowserSetSearch => projectBrowserType.GetMethod("SetSearch", new[] {typeof(string)});


		public static EditorWindow GetProjectBrowserWindow(bool forceOpen = false)
		{
			EditorWindow projectBrowser = EditorWindow.GetWindow(projectBrowserType);
			if (projectBrowser != null)
				return projectBrowser;
			if (!forceOpen)
				return null;
			EditorApplication.ExecuteMenuItem("Window/General/Project");
			return EditorWindow.GetWindow(projectBrowserType);
		}

		#endregion

		#region Header

		public static bool DrawHeader(GUIContent label, SerializedProperty activeField = null, float widthCutoff = 0, bool noBoldOrIndent = false)
		{
			bool ret;
			if (activeField != null)
			{
				activeField.serializedObject.Update();
				bool v = activeField.boolValue;
				ret = DrawHeader(label, ref v, false, widthCutoff, noBoldOrIndent);
				activeField.boolValue = v;
				activeField.serializedObject.ApplyModifiedProperties();
			}
			else
			{
				bool v = true; //Can't wait for c# 7
				ret = DrawHeader(label, ref v, true, widthCutoff, noBoldOrIndent);
			}

			return ret;
		}

		public static bool DrawHeader(GUIContent label, ref bool active, bool hideToggle = false, float widthCutoff = 0, bool noBoldOrIndent = false)
		{
			Rect r = GUILayoutUtility.GetRect(1, 17);
			return DrawHeader(r, label, ref active, hideToggle, widthCutoff, noBoldOrIndent);
		}

		public static bool DrawHeader(Rect contentRect, GUIContent label, ref bool active, bool hideToggle = false, float widthCutoff = 0, bool noBoldOrIndent = false)
		{
			Rect labelRect = contentRect;
			if (!noBoldOrIndent)
				labelRect.xMin += 16f;
			labelRect.xMax -= 20f;
			Rect toggleRect = contentRect;
			if (!noBoldOrIndent)
				toggleRect.xMin = EditorGUI.indentLevel * 15;
			toggleRect.y += 2f;
			toggleRect.width = 13f;
			toggleRect.height = 13f;
			contentRect.xMin = 0.0f;
			EditorGUI.DrawRect(contentRect, HeaderColor);
			using (new EditorGUI.DisabledScope(!active))
				EditorGUI.LabelField(labelRect, label, noBoldOrIndent ? EditorStyles.label : EditorStyles.boldLabel);
			if (!hideToggle)
			{
				active = GUI.Toggle(toggleRect, active, GUIContent.none, SmallTickbox);
				labelRect.xMin = toggleRect.xMax;
			}
			else
				labelRect.xMin = 0;

			Event current = Event.current;
			if (current.type != EventType.MouseDown)
				return false;
			labelRect.width -= widthCutoff;
			if (!labelRect.Contains(current.mousePosition))
				return false;
			if (current.button != 0)
				return false;
			current.Use();
			return true;
		}

		public static bool DrawHeaderWithFoldout(GUIContent label, bool expanded, float widthCutoff = 0, bool opensOnDragUpdated = false)
		{
			bool v = true;
			bool ret = DrawHeader(label, ref v, true, widthCutoff);
			if (Foldout(GUILayoutUtility.GetLastRect(), expanded, opensOnDragUpdated))
				return true;
			return ret;
		}

		private static bool Foldout(Rect r, bool expanded, bool noBoldOrIndent = false)
		{
			switch (Event.current.type)
			{
				case EventType.DragUpdated:
					if (!expanded)
					{
						if (r.Contains(Event.current.mousePosition))
						{
							Event.current.Use();
							return true;
						}
					}

					break;
				case EventType.Repaint:
					//Only draw the Foldout - don't use it as a button or get focus
					r.x += 3;
					if (!noBoldOrIndent)
						r.x += EditorGUI.indentLevel * 15;
					else
						r.xMin -= 16;
					r.y += 1f;
					EditorStyles.foldout.Draw(r, GUIContent.none, -1, expanded);
					break;
			}

			return false;
		}

		public static bool DrawHeaderWithFoldout(Rect rect, GUIContent label, bool expanded,
			float widthCutoff = 0, bool noBoldOrIndent = false)
		{
			bool v = true; //Can't wait for c# 7
			bool ret = DrawHeader(rect, label, ref v, true, widthCutoff, noBoldOrIndent);
			if (Foldout(rect, expanded, noBoldOrIndent))
				return true;
			return ret;
		}

		public static void DrawSplitter(bool inverse = false)
		{
			Rect rect = GUILayoutUtility.GetRect(1f, 1f);
			rect.xMin = 0.0f;
			if (Event.current.type != EventType.Repaint)
				return;
			Color c = inverse ? InverseSplitterColor : SplitterColor;
			c.a = GUI.color.a;
			EditorGUI.DrawRect(rect, c);
		}

		public static Color SplitterColorPro = new Color(0.12f, 0.12f, 0.12f, 1.333f);
		public static Color SplitterColorNonPro = new Color(0.6f, 0.6f, 0.6f, 1.333f);
		public static Color SplitterColor => EditorGUIUtility.isProSkin ? SplitterColorPro : SplitterColorNonPro;
		public static Color InverseSplitterColor => !EditorGUIUtility.isProSkin ? SplitterColorPro : SplitterColorNonPro;
		public static Color HeaderColor => !EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.2f) : new Color(0.1f, 0.1f, 0.1f, 0.2f);

		private static GUIStyle _smallTickbox;
		public static GUIStyle SmallTickbox => _smallTickbox ?? (_smallTickbox = new GUIStyle("ShurikenCheckMark"));

		#endregion

		#region Outline
		public class OutlineScope : IDisposable
		{
			private readonly EditorGUILayout.VerticalScope scope;

			private static GUIStyle _smallMargins;
			private static GUIStyle SmallMargins => _smallMargins ?? (_smallMargins = new GUIStyle(EditorStyles.inspectorDefaultMargins)
			{
				padding = new RectOffset(4, 4, 2, 2),
			});
			
			public OutlineScope(bool drawBackground = true, bool largeMargins = true)
			{
				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
				scope = new EditorGUILayout.VerticalScope(largeMargins ? EditorStyles.inspectorDefaultMargins : SmallMargins);
				Rect rect = scope.rect;
				if (drawBackground)
				{
					if (Event.current.type == EventType.Repaint)
					{
						Color orgColor = GUI.color;
						GUI.color = BackgroundColor;
						GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
						GUI.color = orgColor;
					}
				}

				DrawOutline(new Rect(rect.x, rect.y - 1, rect.width, rect.height + 1), 1);
			}

			public void Dispose()
			{
				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
				scope.Dispose();
			}
		}
		
		private static GUIStyle _headerBackground;
		private static GUIStyle HeaderBackground => _headerBackground ?? (_headerBackground = "RL Header");
		
		private static GUIStyle _boxBackground;
		private static GUIStyle BoxBackground => _boxBackground ?? (_boxBackground = "RL Background");
		
		private static GUIStyle _smallPadding;
		private static GUIStyle SmallPadding => _smallPadding ?? (_smallPadding = new GUIStyle
		{
			padding = new RectOffset(6, 4, 2, 4)
		});

		public static void DrawHeaderWithBackground(GUIContent label)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, HeightWithSpacing);
			if (Event.current.type == EventType.Repaint)
				HeaderBackground.Draw(rect, GUIContent.none, 0);
			rect.Indent(5);
			GUI.Label(rect, label);
		}

		public class ContainerScope : IDisposable
		{
			private readonly int bottomMargin;
			private readonly EditorGUILayout.VerticalScope scope;

			public ContainerScope(GUIContent headerLabel, int bottomMargin = 8)
			{
				this.bottomMargin = bottomMargin;
				DrawHeaderWithBackground(headerLabel);
				scope = new EditorGUILayout.VerticalScope(SmallPadding);
				Rect rect = scope.rect;
				rect.yMin -= 2;
				
				if (Event.current.type == EventType.Repaint)
					BoxBackground.Draw(rect, GUIContent.none, 0);
			}

			public void Dispose()
			{
				scope.Dispose();
				GUILayout.Space(bottomMargin);
			}
		}
		
		public static void DrawOutline(Rect rect, float size)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			Color orgColor = GUI.color;
			GUI.color *= OutlineColor;
			GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, size), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.x, rect.yMax - size, rect.width, size), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.x, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.xMax - size, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);

			GUI.color = orgColor;
		}

		private static Color OutlineColor => EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f);
		private static Color BackgroundColor => EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.9f, 0.9f, 0.9f);

		#endregion

		#region Rect Extensions

		public static void NextGUIRect(this ref Rect rect) => rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;

		public static void Indent(this ref Rect rect, float amount) => rect.xMin += amount;
		#endregion

		public static float HeightWithSpacing => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
	}
}