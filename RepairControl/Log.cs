using System.Collections.Generic;
using System.Linq;

namespace RepairControl
{
    public static class Log
    {
        public static List<string> Items = new List<string>();
        public static void Write(string v)
        {
            lock (Items)
            {
                Items.Add(v);
            }
        }
        public static void WritePacket(string h, byte[] packet)
        {
            if (LogAllPackets)
            {
                Write(packet.Select(b => b.ToString("X2")).ToList().Aggregate(h, (a, b) => a + " " + b));
            }
        }

        public static bool LogAllPackets;
    }
}
