using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using Vertx.Extensions;
using Object = UnityEngine.Object;

namespace Vertx.Editor
{
	internal static class AssetListUtility
	{
		[OnOpenAsset]
		static bool OpenAsset(int instanceID, int line)
		{
			Object @object = EditorUtility.InstanceIDToObject(instanceID);
			if (!(@object is AssetListConfiguration configAsset)) return false;

			AssetListWindow assetListWindow = AssetListWindow.GetUnopenedWindow();
			assetListWindow.InitialiseWithConfiguration(configAsset);
			assetListWindow.Show();
			return true;
		}
		
		public static List<Object> LoadAssetsByTypeName(string assemblyQualifiedName, out Type type, out bool isComponent)
		{
			type = Type.GetType(assemblyQualifiedName);
			isComponent = false;

			List<Object> values = new List<Object>();
			if (type == null)
				return values;

			isComponent = type.IsSubclassOf(typeof(Component));
			if (isComponent)
			{
				GameObject[] prefabs = EditorUtils.LoadAssetsOfType<GameObject>();
				values = new List<Object>();
				foreach (GameObject prefab in prefabs)
					values.AddRange(prefab.GetComponents(type));
				return values;
			}

			string[] guids = AssetDatabase.FindAssets($"t:{type.FullName}");
			if (guids.Length == 0)
			{
				guids = AssetDatabase.FindAssets($"t:{type.Name}");
				if (guids.Length == 0)
					return values;
			}

			foreach (string guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset != null)
					values.Add(asset);
			}
			return values;
		}
	}
}