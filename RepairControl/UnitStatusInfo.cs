namespace RepairControl
{
    public class UnitStatusInfo
    {
        public UnitStatusInfo(byte status, byte jumperValue, byte resistorValue)
        {
            Status = status;
            JumperValue = jumperValue;
            ResistorValue = resistorValue;
        }

        public byte Status { get; private set; }

        public byte JumperValue { get; private set; }

        public byte ResistorValue { get; private set; }

        public bool ResistorStatus
        {
            get { return (Status & 32) == 0; }
        }

        public bool JumpersStatus
        {
            get { return (Status & 64) == 0; }
        }

        public bool RequireData
        {
            get { return (Status & 128) == 128; }
            set { Status = (byte) (Status & (0x7F) | (value ? 128 : 0)); }
        }
    }
}
