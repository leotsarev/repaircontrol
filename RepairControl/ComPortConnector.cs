using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace RepairControl
{
    public class ComPortConnector : IDisposable
    {
        private readonly SerialPort _port;
        private readonly List<Unit> _units = new List<Unit>();
        private readonly IUnitSavedData _savedData;
        private readonly byte _defaultDifficulty;

        public ComPortConnector(string portName, byte defaultDifficulty, IUnitSavedData savedData, bool autoRestore, int autoRepairThreshold)
        {
            _defaultDifficulty = defaultDifficulty;
            Log.Write("Port: " + portName);
            _port = new SerialPort
                       {
                           PortName = portName,
                           BaudRate = 9600,
                           Parity = Parity.None,
                           DataBits = 8,
                           StopBits = StopBits.One,
                           Handshake = Handshake.None,
                           RtsEnable = false, 
                           DtrEnable = false
                       };
            Log.Write("Opening...");
            _port.Open();
            _port.ReadTimeout = 10000;
            _port.WriteTimeout = 10000;
            Log.Write("Set handlers...");
            _port.DataReceived += PortDataReceived;
            _port.ErrorReceived += PortErrorReceived;
           // Log.Write("Set RTS_TOGGLE...");
            //Port.SetDcb(12, 3);
            Log.Write("Completed");
            _savedData = savedData;
            AutoRepairThreshold = autoRepairThreshold;
            AutoRestore = autoRestore;

        }


        static void PortErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Log.Write(string.Format("Port error: {0}", e));
        }

        private readonly List<byte> _buffer = new List<byte>();

        void PortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                lock (_buffer)
                {
                    byte[] buf;
                    lock (_lockPort)
                    {
                        var count = _port.BytesToRead;
                        buf = new byte[count];
                        _port.Read(buf, 0, count);
                    }
                    foreach (var b in buf)
                    {
                        _buffer.Add(b);
                    }
                    try
                    {
                        var command = Command.TryParseCommand(_buffer);
                        if (command != null)
                        {
                            AcceptCommand(command);
                        }
                    }
                    catch (Exception exc)
                    {
                        _buffer.Clear();
                        Log.Write(exc.ToString());
                    }
                }
                
            }
            catch (Exception exception)
            {
                Log.Write(exception.ToString());
            }
        }

        private void AcceptCommand(Command command)
        {
            Log.WritePacket("Client->Server:",command.ToBytes());
            var u = _units.Find(unit => unit.Address == command.Sender);
            if (u == null)
            {
                return;
            }
            command.ApplyToUnit(u);
        }

        public void RegisterUnit(Unit unit)
        {
            _units.Add(unit);
        }

        public void UnRegisterUnit(Unit unit)
        {
            _units.Remove(unit);
        }

        public void Dispose()
        {
            ClosePort();
            GC.SuppressFinalize(this);
        }

        ~ComPortConnector()
        {
            ClosePort();
        }

        private void ClosePort()
        {
            if (_port.IsOpen)
            {
                _port.Close();
            }
        }

        private readonly object _lockPort = new object();

        public void Send(Command command)
        {
            var toSend = command.ToBytes();
            Log.WritePacket("Server->Client:", toSend);
            lock (_lockPort)
            {
                if (_port.BytesToWrite >0)
                {
                    throw new Exception("Write");
                }
                _port.Write(toSend, 0, toSend.Length);
            }
        }

        public void SendPing()
        {
            var first = _units.FirstOrDefault(unit => unit.Dirty);
            if (first != null)
            {
                first.DoSomething();
            }
            Send(Command.CreateBroadCastGet());
        }

        public void Set(UnitStatusData savedData)
        {
            _savedData[savedData.Address] = savedData;
            Send(Command.CreateSetFromSavedData(savedData, _defaultDifficulty));
        }

        public UnitStatusData GetSaved(byte address)
        {
            return _savedData[address];
        }

        public bool AutoRestore { get; private set; }

        public int AutoRepairThreshold { get; private set; }
    }
}
