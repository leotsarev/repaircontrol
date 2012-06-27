namespace RepairControl
{
    public sealed class UnitStatusData
    {
        public readonly byte Address;
        public readonly byte Rvalue;
        public readonly byte Jvalue;

        public UnitStatusData(byte address, byte rvalue, byte jvalue)
        {
            Address = address;
            Rvalue = rvalue;
            Jvalue = jvalue;
        }
    }
}
