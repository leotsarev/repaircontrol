using System;
using System.ComponentModel;
using System.Threading;

namespace RepairControl
{
    public sealed class Unit: INotifyPropertyChanged, IDisposable
    {
        public Unit(byte address, ComPortConnector connector)
        {
            Address = address;
            Connector = connector;
            StatusString = string.Format("{0:X2}", Address);
            LastUpdated = DateTime.MinValue;
            HaveConnection = false;
            Connector.RegisterUnit(this);
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string _statusString1;
        public string StatusString
        {
            get { return _statusString1; }
            private set
            {
                _statusString1 = value;
                OnPropertyChanged("StatusString");
            }
        }

        private DateTime LastUpdated1;
        public DateTime LastUpdated
        {
            get { return LastUpdated1; }
            private set { LastUpdated1 = value;
                OnPropertyChanged("LastUpdated");
                OnPropertyChanged("UpdatedString");
            }
        }

        public string UpdatedString
        {
            get
            {
                return LastUpdated == DateTime.MinValue ? "Никогда" : string.Format("{0:F0} мс", UpdateDelay.TotalMilliseconds);
            }
        }

        private TimeSpan UpdateDelay
        {
            get { return (DateTime.Now - LastUpdated); }
        }

        private bool? ResistorStatus1;
        public bool? ResistorStatus
        {
            get { return ResistorStatus1; }
            private set
            {
                ResistorStatus1 = value; 
                OnPropertyChanged("ResistorStatus"); 
                OnPropertyChanged("ResistorStatusString");
            }
        }

        public string ResistorValueString;

        public string ResistorStatusString
        {
            get {
                return GetStatusString(ResistorStatus);
            }
        }

        private static string GetStatusString(bool? status)
        {
            switch (status)
            {
                case null:
                    return "Неизвестно";
                case false:
                    return "Сломан";
                case true:
                    return "ОК";
                default:
                    throw new Exception("Unknown status");
            }
        }

        public string JumpersStatusString
        {
            get
            {
                return GetStatusString(JumpersStatus);
            }
        }

        private bool? JumpersStatus1;
        public bool? JumpersStatus
        {
            get { return JumpersStatus1; }
            private set { JumpersStatus1 = value; OnPropertyChanged("JumpersStatus"); OnPropertyChanged("JumpersStatusString"); }
        }

        private bool HaveConnection1;
        public string JumperValueString;
        public byte Address;
        private readonly ComPortConnector Connector;
        public UnitStatusInfo StatusInfo;
        private static readonly Random Rnd = new Random((int) (DateTime.Now.Ticks & 0xFFFFFFFF));

        public bool HaveConnection
        {
            get { return HaveConnection1; }
            private set { HaveConnection1 = value; OnPropertyChanged("HaveConnection"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RefreshNow()
        {
            Connector.Send(Command.CreateGet(Address));
        }

        public void BreakResistor()
        {
            ResistorBreakPending = true;
        }

        private UnitStatusData GetUnitStatusData()
        {
            return new UnitStatusData(Address, RequiredResistorByte(RequiredResistorInOms), RequiredJumper);
        }

        private static byte RequiredResistorByte(int oms)
        {
            return (byte) ((256 * oms) / (oms + 10000));
        }

        private static int GetOms(byte x)
        {
            return 10000*x/(256 - x);
        }

        private byte RequiredJumper;
        private int RequiredResistorInOms;

        private readonly int[] ResistorNominals = new[] {1000, 2000, 5100, 6800, 12000, 18000, 15000, 51000};

        public void BreakJumper(byte? jumperValue)
        {
            JumperSettingValue = jumperValue;
            JumperBreakPending = true;
        }

        public static byte GenerateJumperValue()
        {
            var jvalue = (byte) (Rnd.Next(255));
            while (jvalue.CountBits() > 5 || jvalue.CountBits() < 3)
            {
                jvalue = (byte) (Rnd.Next(255));
            }
            return jvalue;
        }

        public void Dispose()
        {
            UnRegisterMe();
            GC.SuppressFinalize(this);
        }

        ~Unit()
        {
            UnRegisterMe();
        }

        private void UnRegisterMe()
        {
            Connector.UnRegisterUnit(this);
        }

        public void HandleUnknown()
        {
        }

        public void HandleGet()
        {
        }


        public void HandleSet(UnitStatusInfo unitStatusInfo)
        {
            StatusInfo = unitStatusInfo;
            RequireData = RequireData || StatusInfo.RequireData;
            JumpersStatus = unitStatusInfo.JumpersStatus;
            ResistorStatus = unitStatusInfo.ResistorStatus;
            //ResistorValueString = string.Format("{0:F1} /{1:F1} кОм", ResistorValueInOms,
            //                                    RequiredResistorInOms/1000.0);
            ResistorValueString = string.Format("{0:F1} энергии", RequiredResistorInOms);
            JumperValueString = StatusInfo.JumperValue.ToBinString() + "/" + RequiredJumper.ToBinString();
            if (Connector.AutoRestore && ShouldBeRestoredForMetro())
            {
                _metroModeRestore = GetOms(StatusInfo.ResistorValue);
            }

            OnPropertyChanged("JumperValue");
            OnPropertyChanged("ResistorValueString");
            AckConn();
        }

        private double ResistorValueInOms
        {
            get { return GetOms(StatusInfo.ResistorValue)/1000.0; }
        }

        private bool ShouldBeRestoredForMetro()
        {
            var resistorValueInOms = GetOms(StatusInfo.ResistorValue);
            bool brokenByResistor = resistorValueInOms != RequiredResistorInOms;
            bool metroCanBeRestored = resistorValueInOms >= Connector.AutoRepairThreshold;
            bool metroNotDisconnected = resistorValueInOms != GetOms(0xFF);
            return brokenByResistor && metroCanBeRestored && metroNotDisconnected;
        }

        public void HandleNAck()
        {
            //HaveConnection = false;
        }

        public void AckConn()
        {
            LastUpdated = DateTime.Now;
            HaveConnection = true;
        }

        private bool ReqDataFlag;
        public bool RequireData
        {
            get { return ReqDataFlag; }
            set { ReqDataFlag = StatusInfo.RequireData = value; }
        }

        public void SetRequiredValues(UnitStatusData savedData)
        {
            RequiredResistorInOms = GetOms(savedData.Rvalue);
            RequiredJumper = savedData.Jvalue;
        }

        private bool JumperBreakPending, ResistorBreakPending;
        private byte? JumperSettingValue;
        private static readonly TimeSpan DelayUntilReset = new TimeSpan(0, 0, 2);
        private int? _metroModeRestore;

        public bool Dirty
        {
            get { return RequireData || JumperBreakPending || ResistorBreakPending || _metroModeRestore != null; }
        }

        public void DoSomething()
        {
            //if (!IsOnline)
            //{
            //    return;
            //}
            if (RequireData)
            {
                var savedData = Connector.GetSaved(Address);
                if (savedData != null)
                {
                    SetRequiredValues(savedData);
                }
                RequireData = false;
            }
            if (JumperBreakPending)
            {
                RequiredJumper = JumperSettingValue ?? GenerateJumperValue();
                JumperBreakPending = false;
            }
            if (ResistorBreakPending)
            {
                RequiredResistorInOms = GenerateResistorValue();
                ResistorBreakPending = false;
            }
            if (_metroModeRestore != null)
            {
                RequiredResistorInOms = (int) _metroModeRestore;
                _metroModeRestore = null;
            }
            Connector.Set(GetUnitStatusData());
            Thread.Sleep(20);
        }

        private int GenerateResistorValue()
        {
            int newResValue = RequiredResistorInOms;
                
            while (RequiredResistorInOms == newResValue)
            {
                newResValue = ResistorNominals[Rnd.Next(ResistorNominals.Length)];    
            }
            return newResValue;
        }

        public bool IsOnline
        {
            get { return HaveConnection && UpdateDelay < DelayUntilReset; }
        }

        public double GetConsumeValue()
        {
            if (StatusInfo == null || ResistorStatus != true || JumpersStatus != true || !IsOnline)
            {
                return 0;
            }
            return ResistorValueInOms;
        }

        public bool IsWorking
        {
            get { return ((JumpersStatus ?? false) && (ResistorStatus ?? false)); }
        }
    }
}
