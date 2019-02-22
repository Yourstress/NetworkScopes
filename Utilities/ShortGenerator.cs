
namespace NetworkScopes
{
	public class ShortGenerator
	{
		bool[] valueStates;
	
		short minimumValue;
		int currentIndex;

		public ShortGenerator (short minimum, short maximum)
		{
			int rangeSize = (maximum - minimum) + 1;
	
			minimumValue = minimum;
			valueStates = new bool[rangeSize];
			currentIndex = 0;
		}

		public short AllocateValue ()
		{
			int start = currentIndex;
			int numValues = valueStates.Length;
	
			for (int x = 0; x < numValues; x++) {
				int vindex = (start + x) % numValues;
	
				// if it's unused, use and return it
				if (!valueStates [vindex]) {
					valueStates [vindex] = true;
	
					// stop at the next one
					currentIndex = vindex + 1;
					return (short)(minimumValue + vindex);
				}
			}
	
			// no usable port found
			throw new System.Exception("Failed to allocate short value because all values are used.");
		}

		public void AllocateManualValue (short value)
		{
			int vindex = value - minimumValue;
	
			// mark it as used
			valueStates [vindex] = true;
		}

		public void DeallocateValue (short value)
		{
			int vindex = value - minimumValue;
	
			// mark it as unused
			valueStates [vindex] = false;
		}
	}
}
