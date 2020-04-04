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

	internal enum MissingPropertyDisplay
	{
		RedWithWarning,
		Blank
	}

	[Flags]
	internal enum AdditionalColumns
	{
		None = 0,
		Path = 1
	}

	internal class AssetListConfiguration : ScriptableObject
	{
		#pragma warning disable 0649
		
		[SerializeField]
		private AssetType assetType = AssetType.InSceneAndAssets;

		public AssetType AssetContext => assetType;

		[SerializeField]
		private MissingPropertyDisplay missingPropertyDisplay;

		public MissingPropertyDisplay MissingPropertyDisplay => missingPropertyDisplay;

		[SerializeField, EnumFlags]
		private AdditionalColumns additionalColumns;

		public AdditionalColumns AdditionalColumns => additionalColumns;
		
		#pragma warning restore 0649

		[Serializable]
		internal class ColumnConfiguration
		{
			#pragma warning disable 0649
			
			[Tooltip("The path to the property that is displayed")]
			public string PropertyPath;

			[Tooltip("The title used in the column's header")]
			public string Title;

			public SerializedPropertyType PropertyType;

			#region Array

			public bool IsArray;

			public ArrayData ArrayPropertyInformation;

			#endregion

			public GUIType DefaultDisplay;

			public NumericalPropertyDisplay NumericalDisplay;

			public EnumPropertyDisplay EnumDisplay;

			public StringPropertyDisplay StringDisplay;

			public ColorPropertyDisplay ColorDisplay;

			public ObjectPropertyDisplay ObjectDisplay;
			
			#pragma warning restore 0649
		}

		[Serializable]
		internal class ArrayData
		{
			#pragma warning disable 0649
			
			public ArrayIndexing ArrayIndexing;

			[Tooltip("The property used in conjunction with the query. The first valid query result will provide the index for the for the drawing array element")]
			public string ArrayPropertyKey;

			/// <summary>
			/// A Regex query on the value as a string (this tooltip is in the label field in <see cref="AssetListConfigurationInspector"/>
			/// </summary>
			public string ArrayQuery;

			[Min(0)]
			public int ArrayIndex;

			[Tooltip("The path to the property that is displayed if the query on the property key is met")]
			public string ArrayPropertyPath;

			public SerializedPropertyType ArrayPropertyType;
			
			#pragma warning restore 0649
		}
		
		#pragma warning disable 0649

		[SerializeField]
		private ColumnConfiguration[] columns;

		public ColumnConfiguration[] Columns => columns;

		[SerializeField, TextArea(1, 5)]
		private string typeString;

		public string TypeString => typeString;

		[SerializeField]
		private NamePropertyDisplay nameDisplay;

		public NamePropertyDisplay NameDisplay => nameDisplay;


		#region Icon

		[SerializeField]
		private string iconPropertyPath;

		public string IconPropertyPath => iconPropertyPath;

		[SerializeField]
		private bool iconIsArray;

		public bool IconIsArray => iconIsArray;

		[SerializeField]
		private ArrayData iconArrayPropertyInformation;

		public ArrayData IconArrayPropertyInformation => iconArrayPropertyInformation;

		#endregion
		
		#pragma warning restore 0649

		public void Configure(Object target)
		{
			Type type = target.GetType();
			typeString = type.AssemblyQualifiedName;
			assetType = type.IsSubclassOf(typeof(Component)) ? AssetType.InSceneAndAssets : AssetType.InAssets;
		}
	}
}