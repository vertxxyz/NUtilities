using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vertx.Editor
{
	[CustomPropertyDrawer(typeof(Object), true)]
	public class BetterObjectFieldDrawer : PropertyDrawer
	{
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			//property.displayName
			ObjectField objectField = new ObjectField("Better");
			return objectField;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			=> Draw(position, property, label);

		public static void Draw(Rect position, SerializedProperty property, GUIContent label)
		{
			Event current = Event.current;
			Rect buttonRect = new Rect(position.xMax - 20, position.y, 20, position.height);
			EditorGUI.DrawRect(buttonRect, Color.red);
			if (current.type == EventType.MouseDown && buttonRect.Contains(current.mousePosition))
			{
				ShowObjectSelectionPopup(property);
				current.Use();
			}

			EditorGUI.ObjectField(position, property, label);
		}

		static void ShowObjectSelectionPopup(SerializedProperty property)
		{
			ObjectSelectionPopup[] windows = Resources.FindObjectsOfTypeAll<ObjectSelectionPopup>();
			foreach (var window in windows)
				window.Close();
			var objectSelectionPopup = EditorWindow.GetWindow<ObjectSelectionPopup>(true, $"Select {property.type.Substring(6, property.type.Length - 7)}", true);
			objectSelectionPopup.Initialise(property);
			objectSelectionPopup.ShowPopup();
		}
	}
}