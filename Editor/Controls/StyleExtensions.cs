using UnityEditor;
using UnityEngine.UIElements;

namespace Vertx.Extensions
{
	public static class StyleExtensions
	{
		public static StyleSheet GetStyleSheet(string name)
		{
			string[] findAssets = AssetDatabase.FindAssets($"t:StyleSheet {name}");
			if (findAssets.Length == 0)
				return null;
			var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(findAssets[0]));
			return sheet;
		}
	}
}