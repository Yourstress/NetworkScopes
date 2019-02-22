
namespace NetworkScopes
{
	public class IntGenerator
	{
		bool[] valueStates;

		int minimumValue;
		int currentIndex;

		public IntGenerator(int minimum, int maximum)
		{
			int rangeSize = (maximum - minimum) + 1;

			minimumValue = minimum;
			valueStates = new bool[rangeSize];
			currentIndex = 0;
		}

		public int AllocateValue()
		{
			int start = currentIndex;
			int numValues = valueStates.Length;

			for (int x = 0; x < numValues; x++)
			{
				int vindex = (start + x) % numValues;

				// if it's unused, use and return it
				if (!valueStates[vindex])
				{
					valueStates[vindex] = true;

					// stop at the next one
					currentIndex = vindex + 1;
					return (minimumValue + vindex);
				}
			}

			// no usable port found
			throw new System.Exception("Failed to allocate int value because all values are used.");
		}

		public void AllocateManualValue(int value)
		{
			int vindex = value - minimumValue;

			// mark it as used
			valueStates[vindex] = true;
		}

		public void DeallocateValue(int value)
		{
			int vindex = value - minimumValue;

			// mark it as unused
			valueStates[vindex] = false;
		}
	}
}
