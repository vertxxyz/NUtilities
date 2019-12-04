using System;

namespace Vertx
{
	public class ProportionalValues
	{
		private float[] values;
		private readonly float total;

		public ProportionalValues(float[] values, float total = 1)
		{
			this.values = values;
			this.total = total;

			float valueTotal = 0;
			foreach (float value in values)
				valueTotal += value;

			if (valueTotal < 0.00001f)
			{
				float toBeDistributedDivided = total / (values.Length - 1);
				for (var i = 0; i < values.Length; i++)
					values[i] = toBeDistributedDivided;
			}
			else
			{
				for (int i = 0; i < values.Length; i++)
					values[i] = (values[i] / valueTotal) * total;
			}
		}

		public float GetValue(int index)
		{
			if (index < 0 || index >= values.Length)
				throw new IndexOutOfRangeException();
			return values[index];
		}

		public void SetValue(int index, float value)
		{
			if (index < 0 || index >= values.Length)
				throw new IndexOutOfRangeException();
			values[index] = value;
			float toBeDistributed = total - value;

			//If there's nothing to be distributed at all
			if (toBeDistributed < 0.00001f)
			{
				//Reset all the other sliders to 0
				for (int i = 0; i < values.Length; i++)
				{
					if (i == index) continue;
					values[i] = 0;
				}

				return;
			}

			//Calculate the total amount of all the other sliders
			float otherValuesTotal = 0;
			for (int i = 0; i < values.Length; i++)
			{
				if (i == index) continue;
				otherValuesTotal += values[i];
			}

			//If none of the values can take up the remaining slack
			if (otherValuesTotal < 0.00001f)
			{
				//just divide it between them all
				float toBeDistributedDivided = toBeDistributed / (values.Length - 1);
				for (int i = 0; i < values.Length; i++)
				{
					if (i == index) continue;
					values[i] = toBeDistributedDivided;
				}

				return;
			}

			//If we have values to be distributed positively
			//ie. values need to move UP
			//[------o---------]
			//[---o------------]
			//[o---------------]
			//[---o------------]
			//Distribute with the remaining bound of the remaining values.
			for (int i = 0; i < values.Length; i++)
			{
				if (i == index) continue;
				values[i] = (values[i] / otherValuesTotal) * toBeDistributed;
			}
		}
	}
}