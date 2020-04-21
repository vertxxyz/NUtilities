using UnityEngine;

namespace Core
{
	public static class DebugUtils
	{
		#region Shared

		public delegate void LineDelegate(Vector3 a, Vector3 b);

		public static Color StartColor => new Color(1f, 0.4f, 0.3f);
		public static Color EndColor => new Color(0.4f, 1f, 0.3f);

		public static Vector3 GetAxisAlignedPerpendicular(Vector3 normal)
		{
			Vector3 alternate = new Vector3(0, 0, 1);
			if (Mathf.Abs(Vector3.Dot(normal, alternate)) > 0.9f)
				alternate = new Vector3(0, 1, 0);
			return alternate;
		}

		#region Shapes

		public static void Circle(Vector3 center, Vector3 normal, float radius, LineDelegate lineDelegate, int segmentCount = 100)
		{
			Vector3 alternate = GetAxisAlignedPerpendicular(normal);
			Vector3 direction = Vector3.Cross(normal, alternate) * radius;
			Vector3 lastPos = center + direction;
			Quaternion rotation = Quaternion.AngleAxis(1 / (float) segmentCount * 360, normal);
			Quaternion currentRotation = rotation;
			for (int i = 1; i <= segmentCount; i++)
			{
				Vector3 nextPos = center + currentRotation * direction;
				lineDelegate(lastPos, nextPos);
				currentRotation = rotation * currentRotation;
				lastPos = nextPos;
			}
		}

		public static void Arc(Vector3 center, Vector3 normal, Vector3 startDirection, float radius, float totalAngle, LineDelegate lineDelegate, int segmentCount = 50)
		{
			Vector3 direction = startDirection * radius;
			Vector3 lastPos = center + direction;
			Quaternion rotation = Quaternion.AngleAxis(1 / (float) segmentCount * totalAngle, normal);
			Quaternion currentRotation = rotation;
			for (int i = 1; i <= segmentCount; i++)
			{
				Vector3 nextPos = center + currentRotation * direction;
				lineDelegate(lastPos, nextPos);
				currentRotation = rotation * currentRotation;
				lastPos = nextPos;
			}
		}

		#endregion

		#endregion

		#region Casts

		#region SphereCast

		public static void DrawSphereCast(
			Vector3 origin,
			float radius,
			Vector3 direction,
			float distance,
			int iterationCount = 10)
			=> DrawSphereCast(origin, radius, direction, distance, StartColor, EndColor, iterationCount);

		public static void DrawSphereCast(
			Vector3 origin,
			float radius,
			Vector3 direction,
			float distance,
			Color colorStart,
			Color colorEnd,
			int iterationCount = 10)
		{
			Vector3 crossA = Vector3.Cross(direction, GetAxisAlignedPerpendicular(direction));
			Vector3 crossB = Vector3.Cross(crossA, direction);
			Color color = colorStart;
			Arc(origin, crossA, crossB, radius, 180, DrawLine);
			Arc(origin, crossB, crossA, radius, -180, DrawLine);

			Vector3 scaledDirection = direction * distance;
			for (int i = 0; i < iterationCount; i++)
			{
				float t = i / ((float) iterationCount - 1);
				color = Color.Lerp(colorStart, colorEnd, t);
				Circle(origin + scaledDirection * t, direction, radius, DrawLine);
			}

			Vector3 end = origin + scaledDirection;
			color = colorEnd;
			Arc(end, crossA, crossB, radius, -180, DrawLine);
			Arc(end, crossB, crossA, radius, 180, DrawLine);

			void DrawLine(Vector3 a, Vector3 b) => Debug.DrawLine(a, b, color);
		}

		#endregion

		#region BoxCast

		public static void DrawBoxCast(
			Vector3 center,
			Vector3 halfExtents,
			Vector3 direction,
			Quaternion orientation,
			float distance,
			int iterationCount = 10)
			=> DrawBoxCast(center, halfExtents, direction, orientation, distance, StartColor, EndColor, iterationCount);

		public static void DrawBoxCast(
			Vector3 center,
			Vector3 halfExtents,
			Vector3 direction,
			Quaternion orientation,
			float distance,
			Color colorStart,
			Color colorEnd,
			int iterationCount = 10)
		{
			Vector3 up = orientation * new Vector3(0, halfExtents.y, 0);
			Vector3 right = orientation * new Vector3(halfExtents.x, 0, 0);
			Vector3 forward = orientation * new Vector3(0, 0, halfExtents.z);

			bool dotUp = Vector3.Dot(up, direction) > 0;
			bool dotRight = Vector3.Dot(right, direction) > 0;
			bool dotForward = Vector3.Dot(forward, direction) > 0;
			
			Color color = colorStart;

			Vector3 uFL_Base = up + forward - right;
			Vector3 uFR_Base = up + forward + right;
			Vector3 uBL_Base = up - forward - right;
			Vector3 uBR_Base = up - forward + right;
			Vector3 dFL_Base = - up + forward - right;
			Vector3 dFR_Base = - up + forward + right;
			Vector3 dBL_Base = - up - forward - right;
			Vector3 dBR_Base = - up - forward + right;

			DrawBox(center, false);

			Vector3 endCenter = center + direction * distance;

			DrawBoxConnectors(center, endCenter);

			color = colorEnd;
			DrawBox(endCenter, true);

			void DrawBox(Vector3 boxCenter, bool inverse)
			{
				Vector3 uFL = uFL_Base + boxCenter;
				Vector3 uFR = uFR_Base + boxCenter;
				Vector3 uBL = uBL_Base + boxCenter;
				Vector3 uBR = uBR_Base + boxCenter;
				Vector3 dFL = dFL_Base + boxCenter;
				Vector3 dFR = dFR_Base + boxCenter;
				Vector3 dBL = dBL_Base + boxCenter;
				Vector3 dBR = dBR_Base + boxCenter;
				
				//up
				DrawLineEnd(uFL, uFR, dotUp, dotForward);
				DrawLineEnd(uFR, uBR, dotUp, dotRight);
				DrawLineEnd(uBR, uBL, dotUp, !dotForward);
				DrawLineEnd(uBL, uFL, dotUp, !dotRight);
				//down
				DrawLineEnd(dFL, dFR, !dotUp, dotForward);
				DrawLineEnd(dFR, dBR, !dotUp, dotRight);
				DrawLineEnd(dBR, dBL, !dotUp, !dotForward);
				DrawLineEnd(dBL, dFL, !dotUp, !dotRight);
				//down to up
				DrawLineEnd(dFL, uFL, dotForward, !dotRight);
				DrawLineEnd(dFR, uFR, dotForward, dotRight);
				DrawLineEnd(dBR, uBR, !dotForward, dotRight);
				DrawLineEnd(dBL, uBL, !dotForward, !dotRight);

				void DrawLineEnd(Vector3 a, Vector3 b, bool bA, bool bB)
				{
					if (inverse)
					{
						if (!bA && !bB) return;
					}
					else
					{
						if (bA && bB) return;
					}

					DrawLine(a, b);
				}
			}

			void DrawBoxConnectors(Vector3 boxCenterA, Vector3 boxCenterB)
			{
				color = Color.Lerp(colorStart, colorEnd, 0.5f);
				bool uFL = DrawConnector(dotUp, dotForward, !dotRight, uFL_Base);
				bool uFR = DrawConnector(dotUp, dotForward, dotRight, uFR_Base);
				bool uBL = DrawConnector(dotUp, !dotForward, !dotRight, uBL_Base);
				bool uBR = DrawConnector(dotUp, !dotForward, dotRight, uBR_Base);
				bool dFL = DrawConnector(!dotUp, dotForward, !dotRight, dFL_Base);
				bool dFR = DrawConnector(!dotUp, dotForward, dotRight, dFR_Base);
				bool dBL = DrawConnector(!dotUp, !dotForward, !dotRight, dBL_Base);
				bool dBR = DrawConnector(!dotUp, !dotForward, dotRight, dBR_Base);

				bool DrawConnector(bool a, bool b, bool c, Vector3 p)
				{
					if (!(a || b || c)) return false;
					if (a && b && c) return false;
					DrawLine(boxCenterA + p, boxCenterB + p);
					return true;
				}
				
				//up
				DrawConnectorIteration(uFL, uFR, uFL_Base, uFR_Base);
				DrawConnectorIteration(uFR, uBR, uFR_Base, uBR_Base);
				DrawConnectorIteration(uBR, uBL, uBR_Base, uBL_Base);
				DrawConnectorIteration(uBL, uFL, uBL_Base, uFL_Base);
				//down
				DrawConnectorIteration(dFL, dFR, dFL_Base, dFR_Base);
				DrawConnectorIteration(dFR, dBR, dFR_Base, dBR_Base);
				DrawConnectorIteration(dBR, dBL, dBR_Base, dBL_Base);
				DrawConnectorIteration(dBL, dFL, dBL_Base, dFL_Base);
				//down to up
				DrawConnectorIteration(dFL, uFL, dFL_Base, uFL_Base);
				DrawConnectorIteration(dFR, uFR, dFR_Base, uFR_Base);
				DrawConnectorIteration(dBR, uBR, dBR_Base, uBR_Base);
				DrawConnectorIteration(dBL, uBL, dBL_Base, uBL_Base);

				void DrawConnectorIteration(bool a, bool b, Vector3 aP, Vector3 bP)
				{
					if (!a || !b) return;
					Vector3 startA = boxCenterA + aP;
					Vector3 startB = boxCenterA + bP;
					Vector3 endA = boxCenterB + aP;
					Vector3 endB = boxCenterB + bP;
					for (int i = 1; i < iterationCount; i++)
					{
						float t = i / (float) iterationCount;
						color = Color.Lerp(colorStart, colorEnd, t);
						DrawLine(Vector3.Lerp(startA, endA, t), Vector3.Lerp(startB, endB, t));
					}
				}
			}

			void DrawLine(Vector3 a, Vector3 b) => Debug.DrawLine(a, b, color);
		}

		#endregion

		#region RaycastHits

		public static void DrawRaycastHits(RaycastHit[] hits, float rayLength = 1, int maxCount = -1, float duration = 0)
			=> DrawRaycastHits(hits, new Color(1, 0.1f, 0.2f), rayLength, maxCount, duration);

		public static void DrawRaycastHits(RaycastHit[] hits, Color color, float rayLength = 1, int maxCount = -1, float duration = 0)
		{
			if (maxCount < 0)
				maxCount = hits.Length;
			for (int i = 0; i < maxCount; i++)
				Debug.DrawRay(hits[i].point, hits[i].normal * rayLength, color, duration);
		}

		#endregion

		#endregion
	}
}