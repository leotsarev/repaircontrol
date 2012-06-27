namespace RepairControl
{
    static class ByteExt 
    {
        public static string ToBinString(this byte b)
        {
            var s = "";
            for (var bitNum = 0; bitNum <8; bitNum ++)
            {
                s += b.HasBit(bitNum) ? "1" : "0";
            }
            return s;
        }

        private static bool HasBit(this byte b, int bitNum)
        {
            return ((b & (1 << bitNum)) != 0);
        }

        public static int CountBits(this byte b)
        {
            var c = 0;
            for (var bitNum = 7; bitNum >= 0; bitNum--)
            {
                if (b.HasBit(bitNum))
                {
                    c++;
                }
            }
            return c;
        }
    }
}
