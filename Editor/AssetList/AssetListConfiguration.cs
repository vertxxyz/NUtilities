using UnityEngine;

namespace Vertx.Editor
{
	internal class AssetListConfiguration : ScriptableObject
	{
		[System.Serializable]
		internal class ColumnConfiguration
		{
			public string PropertyPath;

			public string Title;
		}

		[SerializeField]
		private ColumnConfiguration[] columns;

		public ColumnConfiguration[] Columns => columns;

		[SerializeField]
		private string typeString;

		public string TypeString => typeString;

		public void Configure(Object target) => typeString = target.GetType().AssemblyQualifiedName;
	}
}