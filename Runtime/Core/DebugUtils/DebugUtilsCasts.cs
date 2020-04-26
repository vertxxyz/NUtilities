using UnityEngine;

namespace Core
{
	public static partial class DebugUtils
	{
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
			direction.EnsureNormalized();
			Vector3 crossA = GetAxisAlignedPerpendicular(direction);
			Vector3 crossB = Vector3.Cross(crossA, direction);
			Color color = colorStart;
			DrawArc(origin, crossA, crossB, radius, 180, DrawLine);
			DrawArc(origin, crossB, crossA, radius, -180, DrawLine);

			Vector3 scaledDirection = direction * distance;
			for (int i = 0; i < iterationCount; i++)
			{
				float t = i / ((float) iterationCount - 1);
				color = Color.Lerp(colorStart, colorEnd, t);
				DrawCircleFast(origin + scaledDirection * t, direction, crossA, radius, DrawLine);
			}

			Vector3 end = origin + scaledDirection;
			color = colorEnd;
			DrawArc(end, crossA, crossB, radius, -180, DrawLine);
			DrawArc(end, crossB, crossA, radius, 180, DrawLine);

			void DrawLine(Vector3 a, Vector3 b, float f) => Debug.DrawLine(a, b, color);
		}

		#endregion

		#region BoxCast

		public static void DrawBoxCast(
			Vector3 center,
			Vector3 halfExtents,
			Vector3 direction,
			Quaternion orientation,
			float distance,
			int iterationCount = 1)
			=> DrawBoxCast(center, halfExtents, direction, orientation, distance, StartColor, EndColor, iterationCount);

		public static void DrawBoxCast(
			Vector3 center,
			Vector3 halfExtents,
			Vector3 direction,
			Quaternion orientation,
			float distance,
			Color colorStart,
			Color colorEnd,
			int iterationCount = 1)
		{
			direction.EnsureNormalized();
			
			Vector3 up = orientation * new Vector3(0, halfExtents.y, 0);
			Vector3 right = orientation * new Vector3(halfExtents.x, 0, 0);
			Vector3 forward = orientation * new Vector3(0, 0, halfExtents.z);
			
			Vector3 upNormalised = orientation * new Vector3(0, 1, 0);
			Vector3 rightNormalised = orientation * new Vector3(1, 0, 0);
			Vector3 forwardNormalised = orientation * new Vector3(0, 0, 1);

			float dotUpValue = Vector3.Dot(upNormalised, direction);
			bool dotUp = dotUpValue > 0;
			float dotRightValue = Vector3.Dot(rightNormalised, direction);
			bool dotRight = dotRightValue > 0;
			float dotForwardValue = Vector3.Dot(forwardNormalised, direction);
			bool dotForward = dotForwardValue > 0;

			float dotUpAbsValue = Mathf.Abs(dotUpValue);

			bool aligned = dotUpAbsValue > 0.99999f || dotUpAbsValue < 0.00001f;
			
			Color color = colorStart;

			Vector3 uFL = up + forward - right;
			Vector3 uFR = up + forward + right;
			Vector3 uBL = up - forward - right;
			Vector3 uBR = up - forward + right;
			Vector3 dFL = - up + forward - right;
			Vector3 dFR = - up + forward + right;
			Vector3 dBL = - up - forward - right;
			Vector3 dBR = - up - forward + right;

			DrawBox(center);

			Vector3 endCenter = center + direction * distance;

			DrawBoxConnectors(center, endCenter);

			color = colorEnd;
			DrawBox(endCenter);

			void DrawBox(Vector3 boxCenter)
			{
				Vector3 posUFL = uFL + boxCenter;
				Vector3 posUFR = uFR + boxCenter;
				Vector3 posUBL = uBL + boxCenter;
				Vector3 posUBR = uBR + boxCenter;
				Vector3 posDFL = dFL + boxCenter;
				Vector3 posDFR = dFR + boxCenter;
				Vector3 posDBL = dBL + boxCenter;
				Vector3 posDBR = dBR + boxCenter;

				//up
				DrawLine(posUFL, posUFR);
				DrawLine(posUFR, posUBR);
				DrawLine(posUBR, posUBL);
				DrawLine(posUBL, posUFL);
				//down
				DrawLine(posDFL, posDFR);
				DrawLine(posDFR, posDBR);
				DrawLine(posDBR, posDBL);
				DrawLine(posDBL, posDFL);
				//down to up
				DrawLine(posDFL, posUFL);
				DrawLine(posDFR, posUFR);
				DrawLine(posDBR, posUBR);
				DrawLine(posDBL, posUBL);
			}

			void DrawBoxConnectors(Vector3 boxCenterA, Vector3 boxCenterB)
			{
				if (iterationCount <= 0) return;
				
				if (aligned)
				{
					if (dotUpAbsValue > 0.5f)
					{
						//Up
						bool inverse = dotUpValue < 0;
						DrawConnectorIterationSpecialWithInverse(uFL, uFR, dFL, dFR, inverse);
						DrawConnectorIterationSpecialWithInverse(uFR, uBR, dFR, dBR, inverse);
						DrawConnectorIterationSpecialWithInverse(uBR, uBL, dBR, dBL, inverse);
						DrawConnectorIterationSpecialWithInverse(uBL, uFL, dBL, dFL, inverse);
					}
					else
					{
						//Forward
						float dotForwardAbsValue = Mathf.Abs(dotForwardValue);
						if (dotForwardAbsValue > 0.5f)
						{
							bool inverse = dotForwardValue < 0;
							DrawConnectorIterationSpecialWithInverse(uFL, uFR, uBL, uBR, inverse);
							DrawConnectorIterationSpecialWithInverse(uFR, dFR, uBR, dBR, inverse);
							DrawConnectorIterationSpecialWithInverse(dFR, dFL, dBR, dBL, inverse);
							DrawConnectorIterationSpecialWithInverse(dFL, uFL, dBL, uBL, inverse);
						}
						else
						{
							//Right
							bool inverse = dotRightValue < 0;
							DrawConnectorIterationSpecialWithInverse(uFR, uBR, uFL, uBL, inverse);
							DrawConnectorIterationSpecialWithInverse(uBR, dBR, uBL, dBL, inverse);
							DrawConnectorIterationSpecialWithInverse(dBR, dFR, dBL, dFL, inverse);
							DrawConnectorIterationSpecialWithInverse(dFR, uFR, dFL, uFL, inverse);
						}
					}
				}
				else
				{
					bool validUFL = ValidateConnector(dotUp, dotForward, !dotRight);
					bool validUFR = ValidateConnector(dotUp, dotForward, dotRight);
					bool validUBL = ValidateConnector(dotUp, !dotForward, !dotRight);
					bool validUBR = ValidateConnector(dotUp, !dotForward, dotRight);
					bool validDFL = ValidateConnector(!dotUp, dotForward, !dotRight);
					bool validDFR = ValidateConnector(!dotUp, dotForward, dotRight);
					bool validDBL = ValidateConnector(!dotUp, !dotForward, !dotRight);
					bool validDBR = ValidateConnector(!dotUp, !dotForward, dotRight);

					bool ValidateConnector(bool a, bool b, bool c)
					{
						int count = a ? 1 : 0;
						count += b ? 1 : 0;
						count += c ? 1 : 0;
						if (!aligned)
						{
							if (count == 0)
								return false;
							if (a && b && c) return false;
						}
						else
						{
							if (count != 1)
								return false;
						}
						return true;
					}
					
					//up
					DrawConnectorIteration(validUFL, validUFR, uFL, uFR);
					DrawConnectorIteration(validUFR, validUBR, uFR, uBR);
					DrawConnectorIteration(validUBR, validUBL, uBR, uBL);
					DrawConnectorIteration(validUBL, validUFL, uBL, uFL);
					//down
					DrawConnectorIteration(validDFL, validDFR, dFL, dFR);
					DrawConnectorIteration(validDFR, validDBR, dFR, dBR);
					DrawConnectorIteration(validDBR, validDBL, dBR, dBL);
					DrawConnectorIteration(validDBL, validDFL, dBL, dFL);
					//down to up
					DrawConnectorIteration(validDFL, validUFL, dFL, uFL);
					DrawConnectorIteration(validDFR, validUFR, dFR, uFR);
					DrawConnectorIteration(validDBR, validUBR, dBR, uBR);
					DrawConnectorIteration(validDBL, validUBL, dBL, uBL);
				}

				void DrawConnectorIteration(bool a, bool b, Vector3 aP, Vector3 bP)
				{
					if (!a || !b) return;
					DrawConnectorIterationSpecial(aP, bP, aP, bP);
				}

				void DrawConnectorIterationSpecialWithInverse(Vector3 aPS, Vector3 bPS, Vector3 aPE, Vector3 bPE, bool inverse)
				{
					if (inverse)
						DrawConnectorIterationSpecial(aPE, bPE, aPS, bPS);
					else
						DrawConnectorIterationSpecial(aPS, bPS, aPE, bPE);
				}

				void DrawConnectorIterationSpecial(Vector3 aPS, Vector3 bPS, Vector3 aPE, Vector3 bPE)
				{
					Vector3 startA = boxCenterA + aPS;
					Vector3 startB = boxCenterA + bPS;
					Vector3 endA = boxCenterB + aPE;
					Vector3 endB = boxCenterB + bPE;

					Vector3 currentA = startA;
					Vector3 currentB = startB;

					float diff = 1 / (float)(iterationCount + 1);
					
					for (int i = 1; i < iterationCount; i++)
					{
						float t = i / (float) iterationCount;
						color = Color.Lerp(colorStart, colorEnd, t + diff);
						Vector3 nextA = Vector3.Lerp(startA, endA, t);
						Vector3 nextB = Vector3.Lerp(startB, endB, t);
						
						DrawLine(currentA, nextA);
						DrawLine(currentB, nextB);
						DrawLine(nextA, nextB);
						
						currentA = nextA;
						currentB = nextB;
					}

					color = Color.Lerp(colorStart, colorEnd, 1 - diff);
					DrawLine(currentA, endA);
					DrawLine(currentB, endB);
				}
			}

			void DrawLine(Vector3 a, Vector3 b) => Debug.DrawLine(a, b, color);
		}

		#endregion

		#region RaycastHits

		public static void DrawSphereCastHits(RaycastHit[] hits, float radius, Vector3 forward, int maxCount = -1) =>
			DrawSphereCastHits(hits, radius, forward, new Color(1, 0.1f, 0.2f), maxCount);

		private static void DrawSphereCastHits(RaycastHit[] hits, float radius, Vector3 forward, Color color, int maxCount = -1)
		{
			if (maxCount < 0)
				maxCount = hits.Length;
			
			for (int i = 0; i < maxCount; i++)
			{
				RaycastHit hit = hits[i];
				Vector3 cross = Vector3.Cross(forward, hit.normal);
				DrawCircleFast(hit.point + hit.normal * radius, cross, hit.normal, radius, DrawLine);
				Vector3 secondCross = Vector3.Cross(cross, hit.normal);
				DrawCircleFast(hit.point + hit.normal * radius, secondCross, hit.normal, radius, DrawLine);
			}
			
			void DrawLine(Vector3 a, Vector3 b, float f) => Debug.DrawLine(a, b, new Color(color.r, color.g, color.b, Mathf.Pow(1 - Mathf.Abs(f - 0.5f) * 2, 2) * color.a));
		}

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