// #define VERBOSE_DEBUGGING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using Vertx.Extensions;
using Debug = UnityEngine.Debug;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Vertx.Editor
{
	public enum PackageUpdateType
	{
		Verified,
		Latest
	}

	[CreateAssetMenu(menuName = "Vertx/Package Updater", fileName = "Package Updater")]
	public class PackageUpdater : ScriptableObject
	{
		[Serializable]
		public struct TrackedPackage
		{
			public string Name;
			public string IgnoreVersion;
			public PackageUpdateType UpdateType;

			public string GetVersionToUpgradeTo(PackageInfo packageInfo)
			{
				string newVersion;
				switch (UpdateType)
				{
					case PackageUpdateType.Verified:
					{
						newVersion =
							#if UNITY_2020_1_OR_NEWER
							packageInfo.versions.verified;
							#else
							packageInfo.versions.recommended;
						#endif
						if (string.IsNullOrEmpty(newVersion))
							newVersion = packageInfo.versions.latestCompatible;
						break;
					}
					case PackageUpdateType.Latest:
						newVersion = packageInfo.versions.latestCompatible;
						break;
					default:
						throw new NotImplementedException($"{UpdateType} has not been implemented.");
				}

				return newVersion;
			}

			public bool IsVersionIgnored(string version) => IgnoreVersion.Equals(version);
		}

		[SerializeField] private TrackedPackage[] updatingPackages = null;

		public const string
			updatingPackagesProp = nameof(updatingPackages),
			nameProp = "Name",
			ignoreProp = "IgnoreVersion";

		public List<PackageInfo> CollectUnTrackedPackages(PackageCollection packageCollection)
		{
			//Collect the names of all the packages we're currently tracking and updating.
			HashSet<string> currentlyUpdatingNames = new HashSet<string>();
			foreach (TrackedPackage updatingPackage in updatingPackages)
				currentlyUpdatingNames.Add(updatingPackage.Name);

			return packageCollection.Where(package => !currentlyUpdatingNames.Contains(package.name)).ToList();
		}

		public List<string> CollectUnTrackedPackages(IEnumerable<string> packageNames)
		{
			//Collect the names of all the packages we're currently tracking and updating.
			HashSet<string> currentlyUpdatingNames = new HashSet<string>();
			foreach (TrackedPackage updatingPackage in updatingPackages)
				currentlyUpdatingNames.Add(updatingPackage.Name);

			return packageNames.Where(package => !currentlyUpdatingNames.Contains(package)).ToList();
		}

		public IEnumerable<(TrackedPackage, PackageInfo, int index)> CollectTrackedPackages(PackageCollection packageCollection) =>
			updatingPackages.Select((updatingPackage, index) => (updatingPackage, packageCollection.FirstOrDefault(a => a.name.Equals(updatingPackage.Name)), index));


		#region Update

		private ListRequest request;

		public void UpdateTrackedPackages()
		{
			VerboseLog("Updating Packages ---");

			request = Client.List();
			EditorApplication.update += Progress;
		}

		[Conditional("VERBOSE_DEBUGGING")]
		private static void VerboseLog(object message) => Debug.Log(message);

		private void Progress()
		{
			if (!request.IsCompleted)
				return;
			try
			{
				switch (request.Status)
				{
					case StatusCode.Success:
						UpdateTrackedPackages(request.Result);
						break;
					case StatusCode.InProgress:
						break;
					case StatusCode.Failure:
						Debug.LogError(request.Error.message);
						break;
					default:
						throw new NotImplementedException($"Request status: {request.Status}, not supported.");
				}
			}
			finally
			{
				EditorApplication.update -= Progress;
			}
		}


		public void UpdateTrackedPackages(PackageCollection packageCollection)
		{
			bool UpdateGitPackage(TrackedPackage trackedPackage, PackageInfo packageInfo, int index)
			{
				if (packageInfo.source != PackageSource.Git)
					return false;

				string id = packageInfo.packageId;
				string path = packageInfo.resolvedPath;
				//Resolved data
				string url = id.Substring(id.IndexOf('@') + 1);
				string currentHash = path.Substring(path.IndexOf('@') + 1);

				string[] urlAndBranch = url.Split('#');
				
				string branch;
				if (urlAndBranch.Length == 1)
					branch = "master";
				else
				{
					url = urlAndBranch[0];
					branch = urlAndBranch[1];
				}

				GitUtils.ExecuteGitCommand($"ls-remote {url} {branch}", (success, message) =>
					{
						if (!success)
							return;
						string latestHash = message.Substring(0, message.IndexOf('\t'));
						string packageName = packageInfo.name;
						if (currentHash.Equals(latestHash))
						{
							VerboseLog($"{packageName} {packageInfo.version} is up to date.");
							return;
						}
						
						if (trackedPackage.IsVersionIgnored(latestHash))
						{
							VerboseLog($"Ignored {packageName} {latestHash}");
							return;
						}

						int selection = EditorUtility.DisplayDialogComplex(
							"Package Updater", $"{packageInfo.displayName}\n({packageName}) can be updated.\n{currentHash} to:\n{latestHash}",
							"Update",
							"Ignore Once",
							"Skip Version");
						switch (selection)
						{
							case 0:
								// Update
								Debug.Log(
									$"Updating {packageInfo.displayName} to {packageName} {latestHash} from {currentHash}. This may take a moment and will be delayed as the request occurs.");
								Client.Add(url);
								break;
							case 1:
								// Ignore Once
								VerboseLog($"{packageName} {currentHash} has been ignored once.");
								break;
							case 2:
								// Skip Version
								VerboseLog($"{packageName} {currentHash} has been skipped.");
								TrackedPackage ignorePackage = trackedPackage;
								ignorePackage.IgnoreVersion = currentHash;
								updatingPackages[index] = ignorePackage;
								EditorUtility.SetDirty(this);
								break;
							default:
								throw new NotImplementedException($"Return status: {selection}, from DisplayDialogComplex not supported.");
						}
					},
					true);

				return true;
			}

			IEnumerable<(TrackedPackage, PackageInfo, int index)> trackedPackages = CollectTrackedPackages(packageCollection);
			foreach ((TrackedPackage trackedPackage, PackageInfo packageInfo, int index) in trackedPackages)
			{
				string packageName = trackedPackage.Name;
				string currentVersion = packageInfo.version;
				string updateTo = trackedPackage.GetVersionToUpgradeTo(packageInfo);

				if (string.IsNullOrEmpty(updateTo))
				{
					if (packageInfo.source == PackageSource.Local)
					{
						VerboseLog($"{packageName} {currentVersion} has no latest version. It is a local package.");
						continue;
					}

					if (UpdateGitPackage(trackedPackage, packageInfo, index))
					{
						VerboseLog(
							$"Querying {packageInfo.displayName} - {packageName} via git. This may take a moment as the request occurs.");
						// ReSharper disable once RedundantJumpStatement
						continue;
					}

					VerboseLog($"{packageName} {currentVersion} has no latest version. It may be a package from git, this is not currently supported.");
					continue;
				}

				if (currentVersion.Equals(updateTo))
				{
					VerboseLog($"{packageName} {currentVersion} is up to date.");
					continue;
				}

				if (trackedPackage.IsVersionIgnored(updateTo))
				{
					VerboseLog($"Ignored {packageName} {updateTo}");
					continue;
				}

				SemVersion currentSemVer = SemVersion.Parse(currentVersion);
				SemVersion updateToSemVer = SemVersion.Parse(updateTo);
				if (currentSemVer > updateToSemVer)
				{
					VerboseLog($"{packageName} {currentVersion} is up to date.");
					continue;
				}

				int selection = EditorUtility.DisplayDialogComplex(
					"Package Updater", $"{packageInfo.displayName}\n({packageName}) can be updated.\n{currentVersion} to:\n{updateTo}",
					$"Update to {updateTo}",
					"Ignore Once",
					"Skip Version");
				switch (selection)
				{
					case 0:
						// Update
						Debug.Log(
							$"Updating {packageInfo.displayName} to {packageName} {updateTo} from {currentVersion}. This may take a moment and will be delayed as the request occurs.");
						Client.Add($"{packageName}@{updateTo}");
						break;
					case 1:
						// Ignore Once
						VerboseLog($"{packageName} {updateTo} has been ignored once.");
						break;
					case 2:
						// Skip Version
						VerboseLog($"{packageName} {updateTo} has been skipped.");
						TrackedPackage ignorePackage = trackedPackage;
						ignorePackage.IgnoreVersion = updateTo;
						updatingPackages[index] = ignorePackage;
						EditorUtility.SetDirty(this);
						break;
					default:
						throw new NotImplementedException($"Return status: {selection}, from DisplayDialogComplex not supported.");
				}
			}
		}

		#endregion

		#region Lifetime

		private const double pollRate = 1800;
		private static double timeOfNextUpdate;

		[InitializeOnLoadMethod]
		static void Initialise()
		{
			if (EditorApplication.timeSinceStartup < 60)
				//If this is the first time we're starting, update on the minute.
				timeOfNextUpdate = 60;
			else
				IncrementWait();

			EditorApplication.update += OnUpdate;
		}

		/// <summary>
		/// Sets the next check to the next occurrence of a multiple of poll rate
		/// </summary>
		private static void IncrementWait() => timeOfNextUpdate = EditorApplication.timeSinceStartup - EditorApplication.timeSinceStartup % pollRate + pollRate;

		static void OnUpdate()
		{
			if (timeOfNextUpdate > EditorApplication.timeSinceStartup)
				return;

			IncrementWait();

			PackageUpdater[] packageUpdaters = EditorUtils.LoadAssetsOfType<PackageUpdater>();
			if (packageUpdaters == null) return;
			foreach (PackageUpdater packageUpdater in packageUpdaters)
				packageUpdater.UpdateTrackedPackages();
		}

		#endregion
	}
}
