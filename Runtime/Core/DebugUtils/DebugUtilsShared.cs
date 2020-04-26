using UnityEngine;

namespace Core
{
	public static partial class DebugUtils
	{
		#region Shared

		public delegate void LineDelegate(Vector3 a, Vector3 b, float t);

		public static Color StartColor => new Color(1f, 0.4f, 0.3f);
		public static Color EndColor => new Color(0.4f, 1f, 0.3f);
		
		private static void EnsureNormalized(this ref Vector3 vector3)
		{
			float sqrMag = vector3.sqrMagnitude;
			if (Mathf.Approximately(sqrMag, 1))
				return;
			vector3 /= Mathf.Sqrt(sqrMag);
		}

		private static Vector3 GetAxisAlignedAlternate(Vector3 normal)
		{
			Vector3 alternate = new Vector3(0, 0, 1);
			if (Mathf.Abs(Vector3.Dot(normal, alternate)) > 0.9f)
				alternate = new Vector3(0, 1, 0);
			return alternate;
		}

		public static Vector3 GetAxisAlignedPerpendicular(Vector3 normal)
		{
			Vector3 cross = Vector3.Cross(normal, GetAxisAlignedAlternate(normal));
			cross.EnsureNormalized();
			return cross;
		}

		#region Shapes

		public static void DrawCircle(Vector3 center, Vector3 normal, float radius, LineDelegate lineDelegate, int segmentCount = 100)
		{
			Vector3 cross = GetAxisAlignedPerpendicular(normal);
			Vector3 direction = cross * radius;
			Vector3 lastPos = center + direction;
			Quaternion rotation = Quaternion.AngleAxis(1 / (float) segmentCount * 360, normal);
			Quaternion currentRotation = rotation;
			for (int i = 1; i <= segmentCount; i++)
			{
				Vector3 nextPos = center + currentRotation * direction;
				lineDelegate(lastPos, nextPos, (i - 1) / (float) segmentCount);
				currentRotation = rotation * currentRotation;
				lastPos = nextPos;
			}
		}
		
		public static void DrawCircleFast(Vector3 center, Vector3 normal, Vector3 cross, float radius, LineDelegate lineDelegate, int segmentCount = 100)
		{
			Vector3 direction = cross * radius;
			Vector3 lastPos = center + direction;
			Quaternion rotation = Quaternion.AngleAxis(1 / (float) segmentCount * 360, normal);
			Quaternion currentRotation = rotation;
			for (int i = 1; i <= segmentCount; i++)
			{
				Vector3 nextPos = center + currentRotation * direction;
				lineDelegate(lastPos, nextPos, (i - 1) / (float) segmentCount);
				currentRotation = rotation * currentRotation;
				lastPos = nextPos;
			}
		}

		public static void DrawArc(Vector3 center, Vector3 normal, Vector3 startDirection, float radius, float totalAngle, LineDelegate lineDelegate, int segmentCount = 50)
		{
			Vector3 direction = startDirection * radius;
			Vector3 lastPos = center + direction;
			Quaternion rotation = Quaternion.AngleAxis(1 / (float) segmentCount * totalAngle, normal);
			Quaternion currentRotation = rotation;
			for (int i = 1; i <= segmentCount; i++)
			{
				Vector3 nextPos = center + currentRotation * direction;
				lineDelegate(lastPos, nextPos, (i - 1) / (float) segmentCount);
				currentRotation = rotation * currentRotation;
				lastPos = nextPos;
			}
		}

		#endregion

		#endregion
	}
}