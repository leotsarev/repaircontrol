using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using RepairControl;

namespace RepairControlDb
{
    public class UnitSavedData : Dbc, IUnitSavedData
    {
        private static void Save(UnitStatusData data)
        {
            ExecuteNonQuery("SaveUnitData", Param.Byte("ADDRESS", data.Address), Param.Byte("RVALUE", data.Rvalue),
                            Param.Byte("JVALUE", data.Jvalue));
        }

        readonly Dictionary<byte, UnitStatusData> Units = new Dictionary<byte, UnitStatusData>();
        readonly List<byte> Dirty = new List<byte>();
        private readonly Thread Thread;

        public UnitSavedData()
        {
            foreach (var unitStatusData in from DataRow row in LoadDataTable("LoadUnitData").Rows
                                           select new UnitStatusData((byte) row["ADDRESS"], (byte) row["RVALUE"], (byte) row["JVALUE"]))
            {
                Units.Add(unitStatusData.Address, unitStatusData);
            }

            Thread = new Thread(Worker) {IsBackground = true};
            Thread.Start();
        }

        private readonly AutoResetEvent HasDataToUpdate = new AutoResetEvent(false);

        private void Worker()
        {
            while (HasDataToUpdate.WaitOne())
            {
                foreach (var addr in Dirty.Distinct())
                {
                    Save(Units[addr]);
                }
                Dirty.Clear();
            }
        }

        public UnitStatusData this[byte address]
        {
            get { return Units.ContainsKey(address) ? Units[address] : null; }
            set { Units[address] = value; Dirty.Add(value.Address);
                HasDataToUpdate.Set();
            }
        }
    }
}
