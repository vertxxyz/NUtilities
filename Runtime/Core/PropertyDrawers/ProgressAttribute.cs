using UnityEngine;

namespace Vertx
{
	public class ProgressAttribute : PropertyAttribute
	{
		public readonly float MaxValue;
		public ProgressAttribute(float maxValue) => MaxValue = maxValue;
	}
}