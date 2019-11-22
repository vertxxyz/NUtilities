using UnityEngine;

public class FileAttribute : PropertyAttribute
{
	public readonly bool FileIsLocalToProject;

	public FileAttribute(bool fileIsLocalToProject) => FileIsLocalToProject = fileIsLocalToProject;
}