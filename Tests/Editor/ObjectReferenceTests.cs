using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vertx.Editor.Extensions;
using Object = UnityEngine.Object;

namespace Vertx.Testing.Editor
{
	public class ObjectReferenceTests : ReferenceTests
	{
		[Test]
		public void CheckForMissingReferencesInBuildScenes()
			=> RunFunctionOnSceneObjects(CheckForMissingReferencesOnObject);

		[Test]
		public void CheckForMissingReferencesInAssets()
			=> RunFunctionOnAssets(CheckForMissingReferencesUnderRoot, CheckForMissingReferencesOnObject);

		private static void CheckForMissingReferencesUnderRoot(Object @object) => RunOnComponentsUnderRootGameObjectIgnoringTransform((GameObject) @object, CheckForMissingReferencesOnObject);

		public static void CheckForMissingReferencesOnObject (Object @object)
		{
			SerializedObject serializedObject = new SerializedObject(@object);
			SerializedProperty property = serializedObject.GetIterator();
			while (property.NextVisible(true))
			{
				if (property.propertyType != SerializedPropertyType.ObjectReference)
					continue;
				if (property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0)
					continue;
				string path = EditorUtils.GetPathForObject(@object);

				Assert.Fail($"{path}.{property.propertyPath}\nWas found to be missing.");	
			}
		}
	}
}