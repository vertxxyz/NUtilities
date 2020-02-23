using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Vertx.Extensions {
	public static class EditorUtils {

		#region Assets
		
		public static Object LoadAssetOfType(Type type)
		{
			if(!TryGetGUIDs(out var guids, type))
				return null;
			foreach (string guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset != null && asset.GetType() == type)
					return asset;
			}

			return null;
		}
		
		public static T LoadAssetOfType<T>() where T : Object
		{
			if(!TryGetGUIDs(out var guids, typeof(T)))
				return null;
			foreach (string guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset != null)
					return asset;
			}

			return null;
		}

		public static T[] LoadAssetsOfType<T>() where T : Object
		{
			if(!TryGetGUIDs(out var guids, typeof(T)))
				return Array.Empty<T>();

			List<T> values = new List<T>();
			foreach (string guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
				if(asset != null)
					values.Add(asset);
			}

			return values.ToArray();
		}

		private static bool TryGetGUIDs(out string[] guids, Type type)
		{
			guids = AssetDatabase.FindAssets($"t:{type.FullName}");
			if (guids.Length == 0)
			{
				guids = AssetDatabase.FindAssets($"t:{type.Name}");
				if (guids.Length == 0)
					return false;
			}
			return true;
		}

		#endregion
		
		#region Folders
		public static void ShowFolderContents(int folderInstanceId, bool revealAndFrameInFolderTree)
		{
			Type tProjectBrowser = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
			MethodInfo showContentsMethod =
				tProjectBrowser.GetMethod("ShowFolderContents", BindingFlags.NonPublic | BindingFlags.Instance);
			EditorWindow browser = EditorWindow.GetWindow(tProjectBrowser);
			if (browser != null)
				showContentsMethod.Invoke(browser, new object[] {folderInstanceId, revealAndFrameInFolderTree});
		}
	
		public static int GetMainAssetInstanceID(string path)
		{
			object idObject = typeof(AssetDatabase).GetMethod("GetMainAssetInstanceID", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, new object[] {path});
			if (idObject != null) return (int) idObject;
			return -1;
		}

		public static void ShowFolder(DefaultAsset o)
		{
			if (o == null)
				return;

			string path = AssetDatabase.GetAssetPath (o);
			if (Path.GetFileName(path).Contains("."))
				return; 	//DefaultAsset is a file.
			ShowFolderContents(
				GetMainAssetInstanceID(AssetDatabase.GUIDToAssetPath(AssetDatabase.AssetPathToGUID(path))), true
			);
			EditorWindow.GetWindow(Type.GetType("UnityEditor.ProjectBrowser,UnityEditor")).Repaint();
		}
		#endregion
		
		#region Editor Extensions
		/// <summary>
		/// Returns instances of the types inherited from Type T
		/// </summary>
		/// <typeparam name="T">The type to query and return instances for.</typeparam>
		/// <returns>List of instances inherited from Type T</returns>
		public static List<T> GetEditorExtensionsOfType<T>()
		{
			IEnumerable<Type> derivedTypes = TypeCache.GetTypesDerivedFrom<T>();
			return derivedTypes.Select(t => (T) Activator.CreateInstance(t)).ToList();
		}
		
		/// <summary>
		/// Returns instances of the types inherited from Type T, casted to type TConverted
		/// </summary>
		/// <param name="type">Type query for inheritance</param>
		/// <typeparam name="TConverted">The type to cast new instances to.</typeparam>
		/// <returns>List of instances inherited from Type T</returns>
		public static List<TConverted> GetEditorExtensionsOfType<TConverted>(Type type)
		{
			IEnumerable<Type> derivedTypes = TypeCache.GetTypesDerivedFrom(type);
			return derivedTypes.Select(t => (TConverted) Activator.CreateInstance(t)).ToList();
		}
		#endregion

		#region Property Extensions
		
		private static readonly Dictionary<string, (Type, FieldInfo)> baseTypeLookup = new Dictionary<string, (Type, FieldInfo)>();

		/// <summary>
		/// Gets the backing object from a serialized property.
		/// </summary>
		/// <param name="prop">The property query</param>
		/// <param name="parent">The parent of the returned object</param>
		/// <param name="fieldInfo">The fieldInfo associated with the property</param>
		/// <returns>The object associated with the SerializedProperty <para>prop</para></returns>
		public static object GetObjectFromProperty(SerializedProperty prop, out object parent, out FieldInfo fieldInfo)
		{
			// Separate the steps it takes to get to this property
			string p = Regex.Replace(prop.propertyPath, @".Array.data", string.Empty);
			string[] separatedPaths = p.Split('.');

			// Go down to the root of this serialized property
			object @object = prop.serializedObject.targetObject;
			parent = null;
			fieldInfo = null;
			Type type = prop.serializedObject.targetObject.GetType();
			// Walk down the path to get the target type
			foreach (var pathIterator in separatedPaths)
			{
				int index = -1;
				string path = pathIterator; 
				if (path.EndsWith("]"))
				{
					int startIndex = path.IndexOf('[') + 1;
					int length = path.Length - startIndex - 1;
					index = int.Parse(path.Substring(startIndex, length));
					path = path.Substring(0, startIndex - 1);
				}
					
				fieldInfo = type.GetField(path, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				//Walk up the type tree to find the field in question
				if (fieldInfo == null)
				{
					do
					{
						type = type.BaseType;
						fieldInfo = type.GetField(path, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					} while (fieldInfo == null && type != typeof(object));
				}

				if (fieldInfo == null)
					throw new FieldAccessException($"{type}.{prop.propertyPath} does not have a matching FieldInfo. This is likely because it is a native property.");

				type = fieldInfo.FieldType;
				parent = @object;
				@object = fieldInfo.GetValue(@object);

				if (type.IsArray)
				{					
					if (index >= 0)
					{
						parent = @object;
						@object = ((Array) @object).GetValue(index);
					}
					else if (prop.propertyPath.EndsWith("Array.size"))
					{
						if (@object == null)
							return 0;
						parent = @object;
						@object = ((Array) @object).Length;
						return @object;
					}
					else
						return @object;
					type = @object?.GetType();
				}
				else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
				{
					if (index >= 0)
					{
						parent = @object;
						@object = ((IList) @object)[index];
					}
					else if (prop.propertyPath.EndsWith("Array.size"))
					{
						if (@object == null)
							return 0;
						parent = @object;
						@object = ((IList) @object).Count;
						return @object;
					}
					else
						return @object;
					type = @object?.GetType();
				}
			}

			return @object;
		}

		#endregion
	}
}