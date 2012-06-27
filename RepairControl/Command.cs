using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace RepairControl
{
    public class Command
    {
        private const int ServerAddress = 0x80;
        private enum CommandType
        {
            Unknown = -1,
            Ack = 0,
            NAck = 1,
            Set = 3,
            Get = 4
        }

        [TestFixture]
        public class Tests
        {
            private List<byte> _getCommand;
            private List<byte> _statusCommand;
            private List<byte> _okCommand;

            [SetUp]
            public void Setup()
            {
                _getCommand = new List<byte> { 0x05, ServerAddress, 0x04, 0xA1, 0xD0 };
                _statusCommand = new List<byte> { 0x80, 0x05, 0x3, 0x68, 0xF2, 4, 0x80, 0x9B, 0x8E };
                _okCommand = new List<byte> {0x80, 0x05, 0x00, 0x88, 0x94};
            }

            [Test]
            public void ParseOkCommand()
            {
                var cmd = TryParseCommand(_okCommand);
                Assert.AreEqual(CommandType.Ack, cmd.CmdType);
            }

            [Test]
            public void StatusCommand()
            {
                var command = TryParseCommand(_statusCommand);
                Assert.AreNotEqual(null, command);
            }
            [Test]
            public void GetCommand()
            {
                Assert.AreEqual(_getCommand, CreateGet(5).ToBytes());
            }

            [Test]
            public void ParseGetCommand()
            {
                var command = TryParseCommand(_getCommand);
                Assert.AreNotEqual(null, command);
                Assert.AreEqual(CommandType.Get, command.CmdType);
                Assert.AreEqual(ServerAddress, command.Sender);
            }

            [Test]
            public void ParseIncompleteCommand()
            {
                var buffer = new List<byte>();
                buffer.AddRange(_getCommand);
                buffer.RemoveAt(4);
                var originalBuffer = new List<byte>();
                originalBuffer.AddRange(buffer);
                var command = TryParseCommand(buffer);
                Assert.AreEqual(null, command);
                Assert.AreEqual(originalBuffer, buffer);
            }

            [Test]
            public void ParseGetCommandWithExtra()
            {
                List<byte> buffer = _getCommand;
                buffer.Add(0);
                var command = TryParseCommand(buffer);
                Assert.AreNotEqual(null, command);
                Assert.AreEqual(CommandType.Get, command.CmdType);
                Assert.AreEqual(ServerAddress, command.Sender);
                Assert.AreEqual(new List<byte> {0}, buffer);
            }
        }
        private readonly byte _addr;
        public readonly byte Sender;
        private CommandType CmdType
        {
            get { return (CommandType) _cmd; }
        }

        private readonly byte _cmd;
        private readonly byte _analog;
        private readonly byte _digit;
        private readonly byte _dific;
        private readonly byte _status;

        private Command(byte addr, CommandType cmdType, byte analog = 0, byte digit = 0, byte dific = 0, byte status = 0)
        {
            _addr = addr;
            Sender = ServerAddress;
            _cmd = (byte) cmdType;
            _analog = analog;
            _digit = digit;
            _dific = dific;
            _status = status;
        }

        private Command(IList<byte> command)
        {
            _addr = command[0];
            Sender = command[1];
            _cmd = command[2];
            if (command.Count > 3)
            {
                _analog = command[3];
                _digit = command[4];
                _dific = command[5];
                _status = command[6];
            }
            else
            {
                _analog = _digit = _dific = _status = 0;
            }
        }

        public byte[] ToBytes()
        {
            var len = GetCmdLen(CmdType);
            var buf = new byte[len + 2];
            buf[0] = _addr;
            buf[1] = Sender;
            buf[2] = _cmd;
            if (CmdType == CommandType.Set)
            {
                buf[3] = _analog;
                buf[4] = _digit;
                buf[5] = _dific;
                buf[6] = _status;
            }
            var crc = buf.Calculate(len);
            buf[len + 1] = (byte) (crc >> 8);
            buf[len ] = (byte) (crc & 0xFF);
            return buf;
        }

        private static int GetCmdLen(CommandType commandType)
        {
            return commandType == CommandType.Set ? 7 : 3;
        }

        public static Command CreateGet(byte address)
        {
            return new Command(address, CommandType.Get);
        }

        public static Command TryParseCommand(List<byte> buffer)
        {
            if (buffer.Count < 3)
            {
                return null;
            }
            var cmdLen = GetCmdLen((CommandType) buffer[2]);
            if (buffer.Count < cmdLen + 2)
            {
                return null;
            }
            var crc = GetCrcFromBuffer(buffer, cmdLen);
            var commandBuffer = new byte[cmdLen];
            for (var i = 0; i < cmdLen; i++)
            {
                commandBuffer[i] = buffer[i];
            }
            var calcCrc = commandBuffer.Calculate(cmdLen);
            if (crc != calcCrc)
            {
                buffer.RemoveAt(0);
                return TryParseCommand(buffer);
            }
            buffer.RemoveRange(0, cmdLen + 2);
            
            return new Command(commandBuffer);
        }

        private static ushort GetCrcFromBuffer(IList<byte> buffer, int cmdLen)
        {
            return (ushort) ((buffer[cmdLen + 2-1] << 8) | buffer[cmdLen + 2-2]);
        }

        private static Command CreateSet(byte jvalue, byte rvalue, byte dificulty, byte address)
        {
            return new Command(address, CommandType.Set, rvalue, jvalue, dificulty);
        }

        public void ApplyToUnit(Unit u)
        {
            switch (CmdType)
            {
                
                case CommandType.Ack:
                    u.AckConn();
                    break;
                case CommandType.NAck:
                    u.HandleNAck();
                    break;
                case CommandType.Set:
                    u.HandleSet(new UnitStatusInfo(_status, _digit, _analog));
                    break;
                case CommandType.Get:
                    u.HandleGet();
                    break;
                case CommandType.Unknown:
                    u.HandleUnknown();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Command CreateBroadCastGet()
        {
            return CreateGet(0xF0);
        }

        public static Command CreateSetFromSavedData(UnitStatusData unitStatusData, byte defaultDifficulty)
        {
            return CreateSet(unitStatusData.Jvalue, unitStatusData.Rvalue,
                                     defaultDifficulty, unitStatusData.Address);
        }
    }
}