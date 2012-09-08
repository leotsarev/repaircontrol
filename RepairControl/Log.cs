using System.Collections.Generic;
using System.Linq;

namespace RepairControl
{
    public class Log
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
           Write(packet.Select(b => b.ToString("X2")).ToList().Aggregate(h, (a, b) => a + " " + b));
        }
    }
}
