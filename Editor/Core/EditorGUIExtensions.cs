using UnityEngine;
using UnityEditor;

namespace Vertx {
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
	}
}