using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vertx.Editor.Extensions;

namespace Vertx.Editor.Animations
{
	public class AnimationPaths
	{
		private const string repairAnimationPaths = "Animation - Repair Animation Paths in Animation.";
		
		[ActionProvider]
		private static IEnumerable<ActionOperation> RepairAnimationPaths()
		{
			//Operate On Selection
			Animator[] selectedAnimators = Selection.GetFiltered<Animator>(SelectionMode.Editable);
			if (selectedAnimators.Length != 1) yield break;
			Animator animator = selectedAnimators[0];
			RuntimeAnimatorController controller = animator.runtimeAnimatorController;
			if (controller == null) yield break;
			GameObject root = animator.gameObject;
			
			bool unboundBinding = false;
			foreach (AnimationClip clip in controller.animationClips)
			{
				EditorCurveBinding[] bindingsOnClip = AnimationUtility.GetCurveBindings(clip);
				if (!BindingObjectsExist(bindingsOnClip))
				{
					unboundBinding = true;
					break;
				}

				EditorCurveBinding[] objectReferenceBindingsOnClip = AnimationUtility.GetObjectReferenceCurveBindings(clip);
				if (!BindingObjectsExist(objectReferenceBindingsOnClip))
				{
					unboundBinding = true;
					break;
				}

				bool BindingObjectsExist(EditorCurveBinding[] bindings)
				{
					foreach (EditorCurveBinding binding in bindings)
					{
						Object reference = AnimationUtility.GetAnimatedObject(root, binding);
						if (reference == null)
							return false;
					}

					return true;
				}
			}
			
			if(!unboundBinding) yield break;

			yield return new ActionOperation(
				ActionOperation.ActionTarget.Selection,
				repairAnimationPaths,
				"Animation",
				() =>
				{
					int countReplaced = RepairAnimationPaths(controller, root);
					Debug.Log($"Repaired {countReplaced} AnimationClip bindings in Selection");
				});
		}

		private static int RepairAnimationPaths(RuntimeAnimatorController controller, GameObject root)
		{
			int repaired = 0;

			foreach (AnimationClip clip in controller.animationClips)
			{
				EditorCurveBinding[] bindingsOnClip = AnimationUtility.GetCurveBindings(clip);
				RepairObjectBindings(bindingsOnClip);

				EditorCurveBinding[] objectReferenceBindingsOnClip = AnimationUtility.GetObjectReferenceCurveBindings(clip);
				RepairObjectBindings(objectReferenceBindingsOnClip);

				void RepairObjectBindings(EditorCurveBinding[] bindings)
				{
					foreach (EditorCurveBinding binding in bindings)
					{
						Object reference = AnimationUtility.GetAnimatedObject(root, binding);
						if (reference != null) continue;

						using (var sO = new SerializedObject(clip))
						{
							sO.LogAllProperties();
						}
						
						//TODO repair step
						break;
					}
				}
			}
			
			return repaired;
		}
	}
}