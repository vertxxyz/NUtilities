using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vertx.Editor.Extensions;

namespace Vertx.Testing.Editor
{
	public class AnimationReferenceTests : ReferenceTests
	{
		[Test]
		public void CheckForMissingReferencesInBuildScenes()
			=> RunFunctionOnSceneObjects<Animator>(CheckForMissingAnimationReferences);

		[Test]
		public void CheckForMissingReferencesInAssets()
			=> RunFunctionOnAssets(CheckForMissingAnimationReferences, null);

		public static void CheckForMissingAnimationReferences (Object @object)
		{
			var gameObject = (GameObject) @object;
			if (!gameObject.TryGetComponent(out Animator animator))
				return;

			CheckForMissingAnimationReferences(animator, gameObject);
		}
		
		private static void CheckForMissingAnimationReferences(Animator animator)
			=> CheckForMissingAnimationReferences(animator, animator.gameObject);
			
		private static void CheckForMissingAnimationReferences(Animator animator, GameObject root)
		{
			StringBuilder result = new StringBuilder("The animator on ");
			result.Append(EditorUtils.GetPathForObject(animator));
			result.AppendLine(" has the following missing references:");
			bool failed = false;
			
			RuntimeAnimatorController controller = animator.runtimeAnimatorController;
			if (controller == null) return;
			foreach (AnimationClip clip in controller.animationClips)
			{
				EditorCurveBinding[] bindingsOnClip = AnimationUtility.GetCurveBindings(clip);
				EnsureBindingObjectExists(bindingsOnClip);
				
				EditorCurveBinding[] objectReferenceBindingsOnClip = AnimationUtility.GetObjectReferenceCurveBindings(clip);
				EnsureBindingObjectExists(objectReferenceBindingsOnClip);

				void EnsureBindingObjectExists(EditorCurveBinding[] bindings)
				{
					foreach (EditorCurveBinding binding in bindings)
					{
						Object reference = AnimationUtility.GetAnimatedObject(root, binding);
						if (reference == null)
						{
							failed = true;
							result.Append(clip.name);
							result.Append(" - ");
							result.Append(binding.path);
							result.Append('/');
							result.Append(binding.type.Name);
							result.Append('.');
							result.AppendLine(binding.propertyName);
						}
					}
				}
			}
			
			if(failed)
				Assert.Fail(result.ToString());
		}
	}
}