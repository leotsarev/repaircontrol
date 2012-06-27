namespace RepairControl
{
    public interface IUnitSavedData
    {
        UnitStatusData this[byte address] { get; set; }
    }
}