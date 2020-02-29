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

	internal enum ArrayIndexing
	{
		First,
		ByKey,
		ByIndex
	}

	internal class AssetListConfiguration : ScriptableObject
	{
		[SerializeField]
		private AssetType assetType = AssetType.InSceneAndAssets;

		public AssetType AssetContext => assetType;

		[Serializable]
		internal class ColumnConfiguration
		{
			[Tooltip("The path to the property that is displayed")]
			public string PropertyPath;
			
			[Tooltip("The title used in the column's header")]
			public string Title;

			public SerializedPropertyType PropertyType;

			#region Array

			public bool IsArray;
			public ArrayIndexing ArrayIndexing;
			[Tooltip("The property used in conjunction with the query. The first valid query result will provide the index for the for the drawing array element")]
			public string ArrayPropertyKey;
			[Tooltip("A Regex query on the value as a string")]
			public string ArrayQuery;
			[Min(0)]
			public int ArrayIndex;

			[Tooltip("The path to the property that is displayed if the query on the property key is met")]
			public string ArrayPropertyPath;
			#endregion

			public NumericalPropertyDisplay NumericalDisplay;

			public EnumPropertyDisplay EnumDisplay;

			public StringPropertyDisplay StringDisplay;

			public ColorPropertyDisplay ColorDisplay;
		}

		[SerializeField]
		private ColumnConfiguration[] columns;

		public ColumnConfiguration[] Columns => columns;

		[SerializeField, TextArea(1, 5)]
		private string typeString;

		public string TypeString => typeString;

		[SerializeField]
		private NamePropertyDisplay nameDisplay;
		
		public NamePropertyDisplay NameDisplay => nameDisplay;

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