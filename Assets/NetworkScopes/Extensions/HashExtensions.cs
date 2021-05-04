
namespace NetworkScopes
{
    public static class HashExtensions
    {
        public static int GetConsistentHashCode(this string source)
        {
            int hash1 = 5381;
            int hash2 = hash1;

            int c;
            for (int x = 0; x < source.Length; x+= 2)
            {
                c = source[x];

                hash1 = ((hash1 << 5) + hash1) ^ c;

                if (x+1 < source.Length)
                    c = source[x+1];

                hash2 = ((hash2 << 5) + hash2) ^ c;
            }

            return hash1 + (hash2 * 1566083941);
        }
    }
}