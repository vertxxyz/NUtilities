﻿using UnityEngine;

namespace Vertx
{
	public class MinMaxAttribute : PropertyAttribute
	{
		public readonly float Min;
		public readonly float Max;
		public readonly GUIContent Label;

		public MinMaxAttribute(string label, float min, float max)
		{
			Label = new GUIContent(label);
			Min = min;
			Max = max;
		}
	}
}