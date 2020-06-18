using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Vertx.Editor.Animations
{
	public partial class AnimationPaths
	{
		private const string prependAnimationPaths = "Animation - Prepend To Animation Paths in Animator Controller.";

		[ActionProvider]
		private static IEnumerable<ActionOperation> PrependAnimationPaths()
		{
			//Operate On Selection
			RuntimeAnimatorController[] selectedControllers = Selection.GetFiltered<RuntimeAnimatorController>(SelectionMode.Editable);
			if (selectedControllers.Length == 0)
				yield break;

			yield return new ActionOperation(
				ActionOperation.ActionTarget.Selection,
				prependAnimationPaths,
				"Animation",
				() => PrependAnimationWindow.Open(selectedControllers)
			);
		}

		public class PrependAnimationWindow : EditorWindow
		{
			public RuntimeAnimatorController[] Controllers;
			[SerializeField] private string prependString;

			public static void Open(RuntimeAnimatorController[] controllers)
			{
				var prependAnimationWindow = GetWindow<PrependAnimationWindow>(true, "Prepend To Animations", true);
				prependAnimationWindow.Controllers = controllers;
			}

			private void OnGUI()
			{
				using (new EditorGUI.DisabledScope(true))
				{
					foreach (RuntimeAnimatorController controller in Controllers)
						EditorGUILayout.ObjectField(GUIContent.none, controller, typeof(RuntimeAnimatorController), false);
				}

				prependString = EditorGUILayout.TextField("Prepend", prependString);
				using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(prependString)))
				{
					if (GUILayout.Button("Perform Prepend"))
						PrependAnimationPaths(Controllers);
				}
			}

			private int PrependAnimationPaths(RuntimeAnimatorController[] controllers)
			{
				int prepended = 0;

				foreach (RuntimeAnimatorController controller in controllers)
				{
					foreach (AnimationClip clip in controller.animationClips)
					{
						Undo.RecordObject(clip, "Prepended to Animation Path");

						EditorCurveBinding[] bindingsOnClip = AnimationUtility.GetCurveBindings(clip);
						RepairObjectBindings(bindingsOnClip,
							(oldBinding, newPath) =>
							{
								AnimationCurve animationCurve = AnimationUtility.GetEditorCurve(clip, oldBinding);
								AnimationUtility.SetEditorCurve(clip, oldBinding, null);
								AnimationUtility.SetEditorCurve(clip, new EditorCurveBinding
								{
									path = newPath,
									propertyName = oldBinding.propertyName,
									type = oldBinding.type
								}, animationCurve);
								prepended++;
							}
						);

						EditorCurveBinding[] objectReferenceBindingsOnClip = AnimationUtility.GetObjectReferenceCurveBindings(clip);
						RepairObjectBindings(objectReferenceBindingsOnClip,
							(oldBinding, newPath) =>
							{
								var objectReferenceCurve = AnimationUtility.GetObjectReferenceCurve(clip, oldBinding);
								AnimationUtility.SetObjectReferenceCurve(clip, oldBinding, null);
								AnimationUtility.SetObjectReferenceCurve(clip, new EditorCurveBinding
								{
									path = newPath,
									propertyName = oldBinding.propertyName,
									type = oldBinding.type
								}, objectReferenceCurve);
								prepended++;
							}
						);

						void RepairObjectBindings(EditorCurveBinding[] bindings, Action<EditorCurveBinding, string> action)
						{
							foreach (EditorCurveBinding binding in bindings)
							{
								action.Invoke(binding, string.Concat(prependString, binding.path));
							}
						}
					}
				}

				Debug.Log($"Prepended {prepended} AnimationClip bindings in Selection");
				return prepended;
			}
		}
	}
}