using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vertx.Editor
{
	internal enum GUIType
	{
		Property,
		ReadonlyProperty
	}

	internal enum NumericalPropertyDisplay
	{
		Property,
		Readonly,
		ReadonlyPercentage,
		ReadonlyPercentageNormalised,
		ReadonlyProgress,
		ReadonlyProgressNormalised
	}

	internal class ColumnContext
	{
		private readonly string propertyPath;
		private readonly Action<Rect, SerializedProperty> onGUI;

		public ColumnContext(string propertyPath, string iconPropertyName, AssetListWindow window)
		{
			this.propertyPath = propertyPath;
			onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, iconPropertyName, window);
		}

		public ColumnContext(string propertyPath, GUIType guiType)
		{
			this.propertyPath = propertyPath;
			switch (guiType)
			{
				case GUIType.Property:
					onGUI = Property;
					break;
				case GUIType.ReadonlyProperty:
					onGUI = (rect, property) =>
					{
						using (new EditorGUI.DisabledScope(true))
							Property(rect, property);
					};
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(guiType), guiType, null);
			}
		}

		public ColumnContext(string propertyPath, NumericalPropertyDisplay numericalDisplay)
		{
			this.propertyPath = propertyPath;
			switch (numericalDisplay)
			{
				case NumericalPropertyDisplay.Property:
					onGUI = Property;
					break;
				case NumericalPropertyDisplay.Readonly:
					onGUI = NumericalProperty;
					break;
				case NumericalPropertyDisplay.ReadonlyPercentage:
					onGUI = NumericalPropertyPercentage;
					break;
				case NumericalPropertyDisplay.ReadonlyPercentageNormalised:
					onGUI = NumericalPropertyPercentageNormalised;
					break;
				case NumericalPropertyDisplay.ReadonlyProgress:
					break;
				case NumericalPropertyDisplay.ReadonlyProgressNormalised:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(numericalDisplay), numericalDisplay, null);
			}
		}

		public void OnGUI(Rect cellRect, SerializedProperty @object) => onGUI?.Invoke(cellRect, @object);

		public SerializedProperty GetValue(SerializedObject context) => context.FindProperty(propertyPath);

		public object GetSortableValue(SerializedObject context)
		{
			SerializedProperty property = GetValue(context);
			return AssetListUtility.GetSortableValue(property);
		}

		public void DoTint()
		{
			/*EditorGUI.DrawRect(args.rowRect, new Color(1f, 0f, 0f, 0.15f));
			EditorGUI.DrawRect(args.rowRect, new Color(0f, 0.5f, 1f, 0.15f));
			Color color = GUI.color;
			color.a *= 0.3f;
			GUI.color = color;*/
		}

		#region Default GUIs

		private static void Property(Rect r, SerializedProperty p) => EditorGUI.PropertyField(r, p, GUIContent.none, true);

		private static void LargeObjectLabelWithPing(Rect r, SerializedProperty p, string iconPropertyName, AssetListWindow window)
		{
			Object target = p.serializedObject.targetObject;
			if (!(target is Texture texture))
			{
				if (target is Sprite sprite)
					texture = sprite.texture;
				else
				{
					if (string.IsNullOrEmpty(iconPropertyName))
						texture = null;
					else
					{
						SerializedProperty iconProperty = p.serializedObject.FindProperty(iconPropertyName);
						if (iconProperty == null)
							texture = null;
						else
						{
							Object obj = iconProperty.objectReferenceValue;
							if (obj != null)
							{
								texture = obj as Texture;
								if (texture == null)
									texture = (obj as Sprite)?.texture;
							}
							else
							{
								texture = null;
							}
						}
					}
				}
			}

			Event e = Event.current;

			if (texture != null)
			{
				float h = r.height - 2;
				AssetListUtility.DrawTextureInRect(new Rect(r.x + 10, r.y + 1, h, h), texture);
				if (r.Contains(e.mousePosition) && EditorWindow.focusedWindow == window)
					window.HoveredIcon = texture;
			}

			GUI.Label(
				new Rect(r.x + 10 + r.height, r.y, r.width - 10 - r.height, r.height),
				target.name);
			if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
				EditorGUIUtility.PingObject(target);
		}

		#endregion

		#region Numerical GUIs

		private static void NumericalProperty(Rect r, SerializedProperty p) =>
			GUI.Label(
				r,
				(p.propertyType == SerializedPropertyType.Integer ? p.intValue : p.floatValue).ToString(CultureInfo.InvariantCulture),
				EditorStyles.miniLabel
			);

		private static void NumericalPropertyPercentage(Rect r, SerializedProperty p) =>
			GUI.Label(
				r,
				$"{(p.propertyType == SerializedPropertyType.Integer ? p.intValue : p.floatValue)}%",
				EditorStyles.miniLabel
			);
		
		private static void NumericalPropertyPercentageNormalised(Rect r, SerializedProperty p) =>
			GUI.Label(
				r,
				$"{(p.propertyType == SerializedPropertyType.Integer ? p.intValue : p.floatValue) * 100}%",
				EditorStyles.miniLabel
			);
		
		#endregion
	}
}