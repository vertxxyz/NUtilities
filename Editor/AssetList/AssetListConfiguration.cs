using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vertx.Editor
{
	internal enum AssetType
	{
		InAssets,
		InScene,
		InSceneAndAssets
	}

	internal class AssetListConfiguration : ScriptableObject
	{
		[SerializeField]
		private AssetType assetType = AssetType.InSceneAndAssets;

		public AssetType AssetContext => assetType;

		[Serializable]
		internal class ColumnConfiguration
		{
			public string PropertyPath;

			public string Title;

			public SerializedPropertyType PropertyType;

			public NumericalPropertyDisplay NumericalDisplay;

			public ColorPropertyDisplay ColorDisplay;
		}

		[SerializeField]
		private ColumnConfiguration[] columns;

		public ColumnConfiguration[] Columns => columns;

		[SerializeField, TextArea(1, 5)]
		private string typeString;

		public string TypeString => typeString;

		[SerializeField]
		private string iconPropertyPath;

		public string IconPropertyPath => iconPropertyPath;

		public void Configure(Object target)
		{
			Type type = target.GetType();
			typeString = type.AssemblyQualifiedName;
			assetType = type.IsSubclassOf(typeof(Component)) ? AssetType.InSceneAndAssets : AssetType.InAssets;
		}
	}
}