using RepairControl;

namespace RepairControlPanel
{
    public class NullUnitSavedData : IUnitSavedData
    {
        #region IUnitSavedData Members

        public UnitStatusData this[byte address]
        {
            get { return new UnitStatusData(address, 0, Unit.GenerateJumperValue()); }
            set { } //Do nothing
        }

        #endregion
    }
}