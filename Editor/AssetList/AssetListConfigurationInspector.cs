using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Vertx.Extensions;

namespace Vertx.Editor
{
	[CustomEditor(typeof(AssetListConfiguration))]
	public class AssetListConfigurationInspector : UnityEditor.Editor
	{
		private Type type;
		private bool typeIsTexture;
		
		private SerializedProperty
			columns,
			typeString,
			iconPropertyPath;

		private ReorderableList reorderableList;
		
		private readonly GUIContent
			nameLabel = new GUIContent("Name"),
			iconLabel = new GUIContent("Icon");

		private void OnEnable()
		{
			columns = serializedObject.FindProperty("columns");
			typeString = serializedObject.FindProperty("typeString");
			iconPropertyPath = serializedObject.FindProperty("iconPropertyPath");

			type = Type.GetType(typeString.stringValue);
			
			if (type == null) return;
			
			typeIsTexture = type.IsSubclassOf(typeof(Texture));

			reorderableList = new ReorderableList(serializedObject, columns)
			{
				drawElementCallback = (rect, index, active, focused) =>
				{
					SerializedProperty column = columns.GetArrayElementAtIndex(index);
					SerializedProperty propertyPath = column.FindPropertyRelative("PropertyPath");
					SerializedProperty title = column.FindPropertyRelative("Title");
					rect.height = EditorGUIUtility.singleLineHeight;
					EditorGUI.PropertyField(rect, title);
					rect.NextGUIRect();
					EditorGUI.PropertyField(rect, propertyPath);
				},
				elementHeight = EditorGUIExtensions.HeightWithSpacing * 2
			};
		}

		public override void OnInspectorGUI()
		{
			if (type == null)
			{
				EditorGUILayout.HelpBox("The type associated with this Asset List could not be found.", MessageType.Error);
				EditorGUILayout.PropertyField(typeString);

				return;
			}
			
			serializedObject.UpdateIfRequiredOrScript();
			using (new EditorGUIExtensions.ContainerScope(nameLabel))
			{
				GUILayout.Label(iconLabel, EditorStyles.boldLabel);
				if (typeIsTexture)
					EditorGUILayout.HelpBox("Type inherits from Texture. Icon is automated.", MessageType.Info);
				else
					EditorGUILayout.PropertyField(iconPropertyPath);
			}
			reorderableList.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}