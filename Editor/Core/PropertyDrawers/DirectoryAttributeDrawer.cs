﻿using UnityEngine;
using UnityEditor;
using System.IO;

namespace Vertx
{
	[CustomPropertyDrawer(typeof(DirectoryAttribute))]
	public class DirectoryAttributeDrawer : PropertyDrawer
	{
		/// <summary>
		/// Provide a text field that operates like a button which opens a folder dialogue
		/// A help box is shown when the resulting string is invalid
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			DirectoryAttribute dA = (DirectoryAttribute) attribute;
			Rect r = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(r, property, GUIContent.none);
			r.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			if (string.IsNullOrEmpty(property.stringValue) || !Directory.Exists(property.stringValue))
			{
				string path = dA.DirectoryIsLocalToProject ? "Assets" : Application.dataPath;

				property.stringValue = DirectoryButton(r, property, dA, path);
				r.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				r.height *= 2;
				EditorGUI.HelpBox(r, "Directory is invalid or empty", MessageType.Error);
				property.stringValue = DirectoryButton(default, property, dA, property.stringValue, true);
			}
			else
			{
				property.stringValue = DirectoryButton(r, property, dA, property.stringValue);
			}
		}

		private static string DirectoryButton(Rect position, SerializedProperty sP, DirectoryAttribute dA, string path, bool overPrevious = false)
		{
			if (!overPrevious)
			{
				if (!GUI.Button(position, $"Set {sP.displayName}")) return sP.stringValue;
			}
			else
			{
				if (!EditorGUIExtensions.ButtonOverPreviousControl()) return sP.stringValue;
			}

			string newDirectory = EditorUtility.OpenFolderPanel("Choose Directory", path, path.Equals("Assets") ? string.Empty : path);
			if (string.IsNullOrEmpty(newDirectory))
				return sP.stringValue;
			if (dA.DirectoryIsLocalToProject)
			{
				if (!newDirectory.StartsWith(Application.dataPath))
				{
					Debug.LogWarning("Directory must be local to project, eg. Assets...");
					return sP.stringValue;
				}

				return $"Assets{newDirectory.Substring(Application.dataPath.Length)}";
			}

			return newDirectory;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (string.IsNullOrEmpty(property.stringValue) || !Directory.Exists(property.stringValue))
				return EditorGUIUtility.singleLineHeight * 4 + EditorGUIUtility.standardVerticalSpacing;
			DirectoryAttribute dA = attribute as DirectoryAttribute;
			if (dA.DirectoryIsLocalToProject && property.stringValue.StartsWith(Application.dataPath))
				return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing;
			return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
		}
	}
}