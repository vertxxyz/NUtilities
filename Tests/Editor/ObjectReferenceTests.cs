using System.Text;
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

		private static void CheckForMissingReferencesUnderRoot(Object @object, StringBuilder stringBuilder) =>
			RunOnComponentsUnderRootGameObjectIgnoringTransform((GameObject) @object, stringBuilder, CheckForMissingReferencesOnObject);

		public static void CheckForMissingReferencesOnObject (Object @object, StringBuilder stringBuilder)
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

				stringBuilder.AppendLine($"{path}.{property.propertyPath}\nWas found to be missing.");
			}
		}
	}
}