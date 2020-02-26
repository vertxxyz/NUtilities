using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vertx.Editor
{
	internal enum NamePropertyDisplay
	{
		Label,
		NicifiedLabel,
		CenteredLabel,
		NicifiedCenteredLabel,
	}
	
	internal enum GUIType
	{
		Property,
		ReadonlyProperty
	}

	internal enum NumericalPropertyDisplay
	{
		Property,
		ReadonlyProperty,
		ReadonlyLabel,
		ReadonlyPercentageLabel,
		ReadonlyPercentageLabelNormalised,
		ReadonlyProgressBar,
		ReadonlyProgressBarNormalised
	}

	public enum EnumPropertyDisplay
	{
		Property,
		ReadonlyProperty,
		ReadonlyLabel
	}

	internal enum ColorPropertyDisplay
	{
		Property,
		ReadonlyProperty,
		ReadonlySimplified,
		ReadonlySimplifiedHDR
	}

	internal class ColumnContext
	{
		private readonly string propertyPath;
		private readonly Action<Rect, SerializedProperty> onGUI;

		public ColumnContext(string propertyPath, string iconPropertyName, NamePropertyDisplay nameDisplay, AssetListWindow window)
		{
			this.propertyPath = propertyPath;
			switch (nameDisplay)
			{
				case NamePropertyDisplay.Label:
					onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, iconPropertyName, window, GUI.Label);
					break;
				case NamePropertyDisplay.NicifiedLabel:
					onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, iconPropertyName, window, ReadonlyNicifiedLabelProperty);
					break;
				case NamePropertyDisplay.CenteredLabel:
					onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, iconPropertyName, window, ReadonlyCenteredLabelProperty);
					break;
				case NamePropertyDisplay.NicifiedCenteredLabel:
					onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, iconPropertyName, window, ReadonlyNicifiedCenteredLabelProperty);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(nameDisplay), nameDisplay, null);
			}
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
					onGUI = ReadonlyProperty;
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
				case NumericalPropertyDisplay.ReadonlyProperty:
					onGUI = ReadonlyProperty;
					break;
				case NumericalPropertyDisplay.ReadonlyLabel:
					onGUI = NumericalProperty;
					break;
				case NumericalPropertyDisplay.ReadonlyPercentageLabel:
					onGUI = NumericalPropertyPercentage;
					break;
				case NumericalPropertyDisplay.ReadonlyPercentageLabelNormalised:
					onGUI = NumericalPropertyPercentageNormalised;
					break;
				case NumericalPropertyDisplay.ReadonlyProgressBar:
					onGUI = NumericalPropertyProgressBar;
					break;
				case NumericalPropertyDisplay.ReadonlyProgressBarNormalised:
					onGUI = NumericalPropertyProgressBarNormalised;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(numericalDisplay), numericalDisplay, null);
			}
		}

		public ColumnContext(string propertyPath, EnumPropertyDisplay enumDisplay)
		{
			this.propertyPath = propertyPath;
			switch (enumDisplay)
			{
				case EnumPropertyDisplay.Property:
					onGUI = Property;
					break;
				case EnumPropertyDisplay.ReadonlyProperty:
					onGUI = ReadonlyProperty;
					break;
				case EnumPropertyDisplay.ReadonlyLabel:
					onGUI = ReadonlyEnumProperty;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(enumDisplay), enumDisplay, null);
			}
		}

		public ColumnContext(string propertyPath, ColorPropertyDisplay colorDisplay)
		{
			this.propertyPath = propertyPath;
			switch (colorDisplay)
			{
				case ColorPropertyDisplay.Property:
					onGUI = Property;
					break;
				case ColorPropertyDisplay.ReadonlyProperty:
					onGUI = ReadonlyProperty;
					break;
				case ColorPropertyDisplay.ReadonlySimplified:
					onGUI = (rect, property) => ReadonlyColorSimplified(rect, property, false);
					break;
				case ColorPropertyDisplay.ReadonlySimplifiedHDR:
					onGUI = (rect, property) => ReadonlyColorSimplified(rect, property, true);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(colorDisplay), colorDisplay, null);
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

		private static void ReadonlyProperty(Rect r, SerializedProperty p)
		{
			using (new EditorGUI.DisabledScope(true))
				EditorGUI.PropertyField(r, p, GUIContent.none, true);
		}

		private static void LargeObjectLabelWithPing(
			Rect r,
			SerializedProperty p,
			string iconPropertyName,
			AssetListWindow window,
			Action<Rect, string> labelGUI)
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

			float h = r.height - 2;
			var iconRect = new Rect(r.x + 10, r.y + 1, h, h);
			if (texture != null)
			{
				AssetListUtility.DrawTextureInRect(iconRect, texture);
				if (r.Contains(e.mousePosition) && EditorWindow.focusedWindow == window)
					window.HoveredIcon = texture;
			}

			var labelRect = new Rect(r.x + 10 + r.height, r.y, r.width - 10 - r.height, r.height);
			labelGUI.Invoke(labelRect, target.name);
			if (e.type == EventType.MouseDown && e.button == 0)
			{
				if (labelRect.Contains(e.mousePosition))
				{
					if(target is Component component)
						EditorGUIUtility.PingObject(component.gameObject);
					else
						EditorGUIUtility.PingObject(target);
				}
				else if(iconRect.Contains(e.mousePosition) && texture != null)
					EditorGUIUtility.PingObject(texture);
			}
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
				$"{(p.propertyType == SerializedPropertyType.Integer ? p.intValue : p.floatValue):##0.##}%",
				EditorStyles.miniLabel
			);

		private static void NumericalPropertyPercentageNormalised(Rect r, SerializedProperty p) =>
			GUI.Label(
				r,
				$"{(p.propertyType == SerializedPropertyType.Integer ? p.intValue : p.floatValue) * 100:##0.##}%",
				EditorStyles.miniLabel
			);

		private static void NumericalPropertyProgressBar(Rect r, SerializedProperty p)
		{
			float progress = p.propertyType == SerializedPropertyType.Integer ? p.intValue : p.floatValue;
			EditorGUI.ProgressBar(
				r,
				progress / 100f,
				$"{progress:##0.##}%"
			);
		}

		private static void NumericalPropertyProgressBarNormalised(Rect r, SerializedProperty p)
		{
			float progress = p.propertyType == SerializedPropertyType.Integer ? p.intValue : p.floatValue;
			EditorGUI.ProgressBar(
				r,
				progress,
				$"{progress * 100:##0.##}%"
			);
		}

		#endregion

		#region Enum GUIs

		private static GUIStyle centeredMiniLabel;

		private static GUIStyle CenteredMiniLabel => centeredMiniLabel ?? (centeredMiniLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
		{
			normal =
			{
				textColor = Color.black
			}
		});

		private static void ReadonlyEnumProperty(Rect r, SerializedProperty p)
			=> EditorGUI.LabelField(r, p.enumNames[p.enumValueIndex], CenteredMiniLabel);

		#endregion

		#region Name GUIs
		private static void ReadonlyNicifiedLabelProperty(Rect r, string label)
			=> EditorGUI.LabelField(r, ObjectNames.NicifyVariableName(label));

		private static void ReadonlyCenteredLabelProperty(Rect r, string label)
			=> EditorGUI.LabelField(r, label, CenteredMiniLabel);

		private static void ReadonlyNicifiedCenteredLabelProperty(Rect r, string label)
			=> EditorGUI.LabelField(r, ObjectNames.NicifyVariableName(label), CenteredMiniLabel);

		#endregion

		#region Color GUIs

		private static readonly RectOffset singleOffset = new RectOffset(0, 0, 1, 1);

		private static void ReadonlyColorSimplified(Rect r, SerializedProperty p, bool hdr)
		{
			/*EditorUtils.GetObjectFromProperty(p, out _, out FieldInfo fI);
			bool hdr = fI.GetCustomAttribute<ColorUsageAttribute>()?.hdr ?? false;*/
			Color c = p.colorValue;
			r = singleOffset.Remove(r);
			EditorGUI.ColorField(r, GUIContent.none, c, false, c.a < 1, hdr);
		}

		#endregion
	}
}