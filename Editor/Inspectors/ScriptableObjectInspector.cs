#if !EXCLUDE_SCRIPTABLEOBJECTINSPECTOR

using UnityEditor;
using UnityEngine;
using Vertx.Extensions;

namespace Vertx.Editor
{
	[CustomEditor(typeof(ScriptableObject), true)]
	public class ScriptableObjectInspector : UnityEditor.Editor
	{
		private GUIContent selectContent, searchContent, searchContentSmall;

		protected virtual void OnEnable()
		{
			selectContent = new GUIContent("Select");
			searchContent = new GUIContent($"Search for {target.GetType().Name}");
			searchContentSmall = new GUIContent("Search");
		}

		protected override void OnHeaderGUI()
		{
			base.OnHeaderGUI();
			if (selectContent == null)
			{
				Debug.LogWarning($"base.OnEnable was not called for {GetType().Name}, a class inheriting from {nameof(ScriptableObjectInspector)}.");
				return;
			}
			
			Rect position = GUILayoutUtility.GetLastRect();
			position.y = position.yMax - 21;
			position.height = 15;
			position.xMin += 46;
			position.xMax -= 55;

			Rect selectPosition = position;
			float searchWidth = EditorStyles.miniButton.CalcSize(searchContent).x;
			selectPosition.width = Mathf.Min(position.width / 2f, position.width - searchWidth);


			//Selectively use a small version of the search button when the large version forces the Select button to be too small.
			GUIContent searchContentToUse = searchContent;
			if (selectPosition.width < 60)
			{
				selectPosition.width = 60;
				searchContentToUse = searchContentSmall;
			}

			//Draw the Select button
			if (GUI.Button(selectPosition, selectContent, EditorStyles.miniButtonLeft))
			{
				Selection.activeObject = target;
				EditorGUIUtility.PingObject(target);
			}

			//Draw the Search button
			Rect searchPosition = position;
			searchPosition.xMin = selectPosition.xMax;
			if (GUI.Button(searchPosition, searchContentToUse, EditorStyles.miniButtonRight))
			{
				EditorGUIExtensions.SetProjectBrowserSearch($"t:{target.GetType().FullName}");
			}
		}

		public override void OnInspectorGUI() => DrawDefaultInspector();
	}
}

#endif