using System;
using System.Globalization;
using System.Text.RegularExpressions;
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

	internal enum StringPropertyDisplay
	{
		Property,
		ReadonlyProperty,
		ReadonlyLabel,
		ReadonlyNicifiedLabel,
		ReadonlyCenteredLabel,
		ReadonlyNicifiedCenteredLabel,
	}

	internal enum ColorPropertyDisplay
	{
		Property,
		ReadonlyProperty,
		ReadonlySimplified,
		ReadonlySimplifiedHDR
	}

	internal enum ObjectPropertyDisplay
	{
		Property,
		ReadonlyProperty,
		ReadonlyLabelWithIcon
	}

	internal class ColumnContext
	{
		private readonly string propertyPath;
		private Action<Rect, SerializedProperty> onGUI;
		private readonly Func<SerializedObject, SerializedProperty> getPropertyOverride;

		public ColumnContext(AssetListConfiguration c, NamePropertyDisplay nameDisplay, AssetListWindow window)
		{
			propertyPath = "m_Name";
			Func<SerializedObject, SerializedProperty> getIconProperty = null;
			if (!string.IsNullOrEmpty(c.IconPropertyPath))
			{
				if (c.IconIsArray)
				{
					AssetListConfiguration.ArrayData propInfo = c.IconArrayPropertyInformation;
					Func<SerializedProperty, SerializedProperty> arrayLookup = GetArrayPropertyLookup(propInfo);
					if (arrayLookup != null)
					{
						getIconProperty = context =>
						{
							SerializedProperty iconPath = context.FindProperty(c.IconPropertyPath);
							return arrayLookup?.Invoke(iconPath);
						};
					}
				}
				else
				{
					getIconProperty = context => context.FindProperty(c.IconPropertyPath);
				}
			}

			switch (nameDisplay)
			{
				case NamePropertyDisplay.Label:
					onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, getIconProperty, window, GUI.Label);
					break;
				case NamePropertyDisplay.NicifiedLabel:
					onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, getIconProperty, window, ReadonlyNicifiedLabelProperty);
					break;
				case NamePropertyDisplay.CenteredLabel:
					onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, getIconProperty, window, ReadonlyCenteredLabelProperty);
					break;
				case NamePropertyDisplay.NicifiedCenteredLabel:
					onGUI = (rect, property) => LargeObjectLabelWithPing(rect, property, getIconProperty, window, ReadonlyNicifiedCenteredLabelProperty);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(nameDisplay), nameDisplay, null);
			}
		}
		
		/// <summary>
		/// Constructor for Path
		/// </summary>
		public ColumnContext(AssetListConfiguration configuration, AssetListWindow window)
		{
			propertyPath = string.Empty;
			Texture2D persistent = null;//TODO assign appropriate icon
			Texture2D inScene = null;//TODO assign appropriate icon
			Texture2D GetIcon(Object o) => EditorUtility.IsPersistent(o) ? persistent : inScene;
			onGUI = (rect, property) => PathLabelWithIcon(rect, property, (Func<Object, Texture2D>) GetIcon);
		}

		public ColumnContext(AssetListConfiguration.ColumnConfiguration c)
		{
			propertyPath = c.PropertyPath;
			if (!c.IsArray)
			{
				ConfigureGUI(c.PropertyType);
				return;
			}

			void ConfigureGUI(SerializedPropertyType propertyType)
			{
				switch (propertyType)
				{
					case SerializedPropertyType.Float:
					case SerializedPropertyType.Integer:
						switch (c.NumericalDisplay)
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
								throw new ArgumentOutOfRangeException(nameof(c.NumericalDisplay), c.NumericalDisplay, null);
						}

						break;
					case SerializedPropertyType.Enum:
						switch (c.EnumDisplay)
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
								throw new ArgumentOutOfRangeException(nameof(c.EnumDisplay), c.EnumDisplay, null);
						}

						break;
					case SerializedPropertyType.String:
						switch (c.StringDisplay)
						{
							case StringPropertyDisplay.Property:
								onGUI = Property;
								break;
							case StringPropertyDisplay.ReadonlyProperty:
								onGUI = ReadonlyProperty;
								break;
							case StringPropertyDisplay.ReadonlyLabel:
								onGUI = (rect, property) => GUI.Label(rect, property.stringValue);
								break;
							case StringPropertyDisplay.ReadonlyNicifiedLabel:
								onGUI = (rect, property) => ReadonlyNicifiedLabelProperty(rect, property.stringValue);
								break;
							case StringPropertyDisplay.ReadonlyCenteredLabel:
								onGUI = (rect, property) => ReadonlyCenteredLabelProperty(rect, property.stringValue);
								break;
							case StringPropertyDisplay.ReadonlyNicifiedCenteredLabel:
								onGUI = (rect, property) => ReadonlyNicifiedCenteredLabelProperty(rect, property.stringValue);
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(c.StringDisplay), c.StringDisplay, null);
						}

						break;
					case SerializedPropertyType.Color:
						switch (c.ColorDisplay)
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
								throw new ArgumentOutOfRangeException(nameof(c.ColorDisplay), c.ColorDisplay, null);
						}

						break;
					case SerializedPropertyType.ObjectReference:
						switch (c.ObjectDisplay)
						{
							case ObjectPropertyDisplay.Property:
								onGUI = Property;
								break;
							case ObjectPropertyDisplay.ReadonlyProperty:
								onGUI = ReadonlyProperty;
								break;
							case ObjectPropertyDisplay.ReadonlyLabelWithIcon:
								onGUI = (rect, property) =>
								{
									if (property.objectReferenceValue == null)
										GUI.Label(rect, "Null");
									else
										GUI.Label(rect, EditorGUIUtility.ObjectContent(property.objectReferenceValue, null));
								};
								break;
							default:
								throw new ArgumentOutOfRangeException(nameof(c.ObjectDisplay), c.ObjectDisplay, null);
						}

						break;
					case SerializedPropertyType.Generic:
						throw new NotImplementedException($"{SerializedPropertyType.Generic} is not supported without being an array.");
					case SerializedPropertyType.Boolean:
					case SerializedPropertyType.LayerMask:
					case SerializedPropertyType.Vector2:
					case SerializedPropertyType.Vector3:
					case SerializedPropertyType.Vector4:
					case SerializedPropertyType.Rect:
					case SerializedPropertyType.ArraySize:
					case SerializedPropertyType.Character:
					case SerializedPropertyType.AnimationCurve:
					case SerializedPropertyType.Bounds:
					case SerializedPropertyType.Gradient:
					case SerializedPropertyType.Quaternion:
					case SerializedPropertyType.ExposedReference:
					case SerializedPropertyType.FixedBufferSize:
					case SerializedPropertyType.Vector2Int:
					case SerializedPropertyType.Vector3Int:
					case SerializedPropertyType.RectInt:
					case SerializedPropertyType.BoundsInt:
					#if UNITY_2019_3_OR_NEWER
					case SerializedPropertyType.ManagedReference:
					#endif
					default:
						switch (c.DefaultDisplay)
						{
							case GUIType.Property:
								onGUI = Property;
								break;
							case GUIType.ReadonlyProperty:
								onGUI = ReadonlyProperty;
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						break;
				}
			}

			// Configuration for an array property -------------------------------

			AssetListConfiguration.ArrayData propInfo = c.ArrayPropertyInformation;
			Func<SerializedProperty, SerializedProperty> getProperty = GetArrayPropertyLookup(propInfo);

			getPropertyOverride = context =>
			{
				SerializedProperty property = context.FindProperty(propertyPath);
				return getProperty.Invoke(property);
			};

			ConfigureGUI(propInfo.ArrayPropertyType);
		}

		private static Func<SerializedProperty, SerializedProperty> GetArrayPropertyLookup(AssetListConfiguration.ArrayData propInfo)
		{
			Func<SerializedProperty, SerializedProperty> getProperty;
			string arrayPropertyPath = propInfo.ArrayPropertyPath;
			switch (propInfo.ArrayIndexing)
			{
				case ArrayIndexing.First:
					getProperty = arrayProperty =>
					{
						if (arrayProperty.arraySize == 0)
							return null;
						SerializedProperty element = arrayProperty.GetArrayElementAtIndex(0);
						return element?.FindPropertyRelative(arrayPropertyPath);
					};
					break;
				case ArrayIndexing.ByKey:
					Regex regex = new Regex(propInfo.ArrayQuery);
					string arrayPropertyKey = propInfo.ArrayPropertyKey;
					getProperty = arrayProperty =>
					{
						if (arrayProperty == null) return null;
						for (int i = 0; i < arrayProperty.arraySize; i++)
						{
							SerializedProperty property = arrayProperty.GetArrayElementAtIndex(i);
							SerializedProperty key = property.FindPropertyRelative(arrayPropertyKey);

							string keyForRegex = AssetListUtility.GetValueForRegex(key);
							if (!regex.IsMatch(keyForRegex)) continue;

							return property.FindPropertyRelative(arrayPropertyPath);
						}

						return null;
					};
					break;
				case ArrayIndexing.ByIndex:
					int index = propInfo.ArrayIndex;
					getProperty = arrayProperty =>
					{
						if (arrayProperty.arraySize <= index)
							return null;
						SerializedProperty element = arrayProperty.GetArrayElementAtIndex(index);
						return element?.FindPropertyRelative(arrayPropertyPath);
					};
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return getProperty;
		}

		public void OnGUI(Rect cellRect, SerializedProperty @object) => onGUI?.Invoke(cellRect, @object);

		public SerializedProperty GetValue(SerializedObject context) => getPropertyOverride != null ? getPropertyOverride(context) : context.FindProperty(propertyPath);

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

		private static void Property(Rect r, SerializedProperty p) => EditorGUI.PropertyField(r, p, GUIContent.none, false);

		private static void ReadonlyProperty(Rect r, SerializedProperty p)
		{
			using (new EditorGUI.DisabledScope(true))
				EditorGUI.PropertyField(r, p, GUIContent.none, false);
		}

		private static void LargeObjectLabelWithPing(
			Rect r,
			SerializedProperty p,
			Func<SerializedObject, SerializedProperty> getIconProperty,
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
					if (getIconProperty == null)
						texture = null;
					else
					{
						SerializedProperty iconProperty = getIconProperty(p.serializedObject);
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
					if (target is Component component)
						EditorGUIUtility.PingObject(component.gameObject);
					else
						EditorGUIUtility.PingObject(target);
				}
				else if (iconRect.Contains(e.mousePosition) && texture != null)
					EditorGUIUtility.PingObject(texture);
			}
		}

		#endregion

		#region Name GUIs

		private static void ReadonlyNicifiedLabelProperty(Rect r, string label)
			=> EditorGUI.LabelField(r, ObjectNames.NicifyVariableName(label));

		private static void ReadonlyCenteredLabelProperty(Rect r, string label)
			=> EditorGUI.LabelField(r, label, CenteredMiniLabel);

		private static void ReadonlyNicifiedCenteredLabelProperty(Rect r, string label)
			=> EditorGUI.LabelField(r, ObjectNames.NicifyVariableName(label), CenteredMiniLabel);

		#endregion

		#region Path GUI

		private static void PathLabelWithIcon(Rect rect, SerializedProperty property, Func<Object, Texture2D> getIcon)
		{
			throw new NotImplementedException();
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