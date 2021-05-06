
namespace NetworkScopes
{
    public class ChannelGenerator
    {
        bool[] valueStates;

        short minimumValue;
        int currentIndex;

        public ChannelGenerator (short minimum, short maximum)
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

            for (int x = 0; x < numValues; x++)
            {
                int vindex = (start + x) % numValues;

                short value = (short) (minimumValue + vindex);
                
                // skip system channels
                if (ScopeChannel.IsSystemScope(value))
                {
                    start = ScopeChannel.LastSystemChannel;
                    x = 0;
                    continue;
                }

                // if it's unused, use and return it
                if (!valueStates [vindex])
                {
                    valueStates [vindex] = true;

                    // stop at the next one
                    currentIndex = vindex + 1;
                    return value;
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
