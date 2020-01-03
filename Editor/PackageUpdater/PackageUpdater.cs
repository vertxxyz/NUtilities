using UnityEditor;
using UnityEngine;

namespace Vertx.Editor
{
	[CreateAssetMenu(menuName = "Vertx/Package Updater", fileName = "Package Updater")]
	public class PackageUpdater : ScriptableObject
	{
		[System.Serializable]
		public struct PackageInfo
		{
			public string Name;
			public string IgnoreVersion;
		}

		[SerializeField] private PackageInfo[] updatingPackages = null;
		
		public const string
			updatingPackagesProp = nameof(updatingPackages),
			nameProp = "Name",
			ignoreProp = "IgnoreVersion";
		
		public void SetToSkipPackageVersion(string packageName, string versionToIgnore)
		{
			for (var i = 0; i < updatingPackages.Length; i++)
			{
				PackageInfo packageInfo = updatingPackages[i];
				if (!packageInfo.Name.Equals(packageName)) continue;
				packageInfo.IgnoreVersion = versionToIgnore;
				updatingPackages[i] = packageInfo;
				EditorUtility.SetDirty(this);
				return;
			}

			Debug.LogWarning($"{packageName} ({versionToIgnore}) was not ignored.");
		}

		public int GetIndexOf(string packageName)
		{
			for (int i = 0; i < updatingPackages.Length; i++)
			{
				if (updatingPackages[i].Name.Equals(packageName))
					return i;
			}
			return -1;
		}

		public bool IsVersionIgnored(string packageName, string version)
		{
			foreach (PackageInfo updatingPackage in updatingPackages)
			{
				if(!updatingPackage.Name.Equals(packageName)) continue;
				return updatingPackage.IgnoreVersion.Equals(version);
			}
			return false;
		}
	}
}