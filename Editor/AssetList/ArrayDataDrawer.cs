using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Vertx.Extensions;
using Object = UnityEngine.Object;
using static Vertx.Editor.AssetListUtility;

namespace Vertx.Editor
{
	public static class ArrayDataDrawer
	{
		private static readonly GUIContent
			addArrayKeyLabel = new GUIContent("Add Key", "Find a Serialized Property to use as a key into the above array property."),
			addDrawingPropertyLabel = new GUIContent("Add Drawing Property", "Find a Serialized Property to draw if the key's query has passed."),
			queryLabel = new GUIContent("Query", "A Regex query on the value as a string");
		
		public static void OnGUI(ref Rect rect, SerializedProperty propertyPath, SerializedProperty arrayProperty, Object referenceObject)
		{
			SerializedProperty indexing = arrayProperty.FindPropertyRelative("ArrayIndexing");
			rect.NextGUIRect();
			EditorGUI.PropertyField(rect, indexing);
			
			if (!AssetListConfigurationInspector.ValidateReferenceObjectWithHelpWarningRect(referenceObject, ref rect)) return;
			
			switch ((ArrayIndexing) indexing.intValue)
			{
				case ArrayIndexing.First:
					DrawDrawingProperty(ref rect);
					break;
				case ArrayIndexing.ByKey:
					//The property to look for as a key. This is associated with the query.
					SerializedProperty key = arrayProperty.FindPropertyRelative("ArrayPropertyKey");
					bool hasKey = !string.IsNullOrEmpty(key.stringValue);
					rect.NextGUIRect();
					GUI.Label(rect, hasKey ? "Key and Query" : "Key", EditorStyles.boldLabel);
					rect.NextGUIRect();
					using (new EditorGUI.DisabledScope(true))
						EditorGUI.PropertyField(rect, key, GUIContent.none);
					rect.NextGUIRect();
					if (GUI.Button(rect, addArrayKeyLabel))
					{
						//retrieves properties under the first array index for the dropdown
						DisplayArrayKeyPropertyDropdown(rect, $"{propertyPath.stringValue}.Array.data[0]", arrayProperty, referenceObject);
					}

					//The query into the array. This is associated with the array property key.
					if (!string.IsNullOrEmpty(key.stringValue))
					{
						rect.NextGUIRect();
						SerializedProperty arrayQuery = arrayProperty.FindPropertyRelative("ArrayQuery");
						EditorGUI.PropertyField(rect, arrayQuery, queryLabel);

						//The property to draw if the query has passed.
						if (!string.IsNullOrEmpty(arrayQuery.stringValue))
							DrawDrawingProperty(ref rect);
					}

					break;
				case ArrayIndexing.ByIndex:
					rect.NextGUIRect();
					EditorGUI.PropertyField(rect, arrayProperty.FindPropertyRelative("ArrayIndex"));
					DrawDrawingProperty(ref rect);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			void DrawDrawingProperty(ref Rect r)
			{
				r.NextGUIRect();
				GUI.Label(r, "Property", EditorStyles.boldLabel);
				r.NextGUIRect();
				SerializedProperty arrayPropertyPath = arrayProperty.FindPropertyRelative("ArrayPropertyPath");
				using (new EditorGUI.DisabledScope(true))
					EditorGUI.PropertyField(r, arrayPropertyPath);
				r.NextGUIRect();
				if (GUI.Button(r, addDrawingPropertyLabel))
					DisplayArrayDrawingPropertyDropdown(r, $"{propertyPath.stringValue}.Array.data[0]", arrayProperty, referenceObject);
			}
		}
		
		private static void DisplayArrayKeyPropertyDropdown(Rect rect, string propertyPath, SerializedProperty column, Object referenceObject)
		{
			HashSet<string> propertyPaths = new HashSet<string>();

			var iterator = new ScriptableObjectPropertyIterator(referenceObject, propertyPath);
			if (!iterator.IsValid())
			{
				Debug.LogError($"The current reference object {referenceObject} does not contain an array of this type with values in it. Try another reference object.");
				return;
			}
			
			foreach (SerializedProperty property in iterator)
			{
				if(!IsValidPropertyKeyType(property.propertyType)) continue;
				propertyPaths.Add(property.propertyPath.Substring(propertyPath.Length + 1));// + 1 to skip the '.'
			}

			PropertyDropdown dropdown = new PropertyDropdown(new AdvancedDropdownState(), s =>
			{
				column.FindPropertyRelative("ArrayPropertyKey").stringValue = s;
				column.serializedObject.ApplyModifiedProperties();
			}, propertyPaths, null);
			dropdown.Show(rect);
		}
		
		private static void DisplayArrayDrawingPropertyDropdown(Rect rect, string propertyPath, SerializedProperty column, Object referenceObject)
		{
			HashSet<string> propertyPaths = new HashSet<string>();
			Dictionary<string, SerializedPropertyType> typeLookup = new Dictionary<string, SerializedPropertyType>();

			var iterator = new ScriptableObjectPropertyIterator(referenceObject, propertyPath);
			if (!iterator.IsValid())
			{
				Debug.LogError($"The current reference object {referenceObject} does not contain an array of this type with values in it. Try another reference object.");
				return;
			}
			
			foreach (SerializedProperty property in iterator)
			{
				if(property.propertyType == SerializedPropertyType.Generic) continue;
				string localPath = property.propertyPath.Substring(propertyPath.Length + 1);
				propertyPaths.Add(localPath);// + 1 to skip the '.'
				typeLookup.Add(localPath, property.propertyType);
			}

			PropertyDropdown dropdown = new PropertyDropdown(new AdvancedDropdownState(), s =>
			{
				column.FindPropertyRelative("ArrayPropertyPath").stringValue = s;
				column.FindPropertyRelative("ArrayPropertyType").intValue = (int) typeLookup[s];
				column.serializedObject.ApplyModifiedProperties();
			}, propertyPaths, null);
			dropdown.Show(rect);
		}
		
		private class ScriptableObjectPropertyIterator : IEnumerable<SerializedProperty>
		{
			private readonly SerializedObject serializedObject;
			private readonly SerializedProperty prop;

			public ScriptableObjectPropertyIterator(Object referenceObject, string parentPropertyPath)
			{
				serializedObject = new SerializedObject(referenceObject);
				typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(serializedObject, 1);
				prop = serializedObject.FindProperty(parentPropertyPath);
			}

			public bool IsValid() => prop != null;

			public IEnumerator<SerializedProperty> GetEnumerator()
			{
				SerializedProperty end = prop.GetEndProperty();
				while (prop.NextVisible(true) && !SerializedProperty.EqualContents(prop, end))
					yield return prop;
				serializedObject.Dispose();
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}
}