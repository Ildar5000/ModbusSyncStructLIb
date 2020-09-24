using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
using Modbus.Device;

namespace ModbusSyncStructLIb
{
    public class MasterSyncStruct
    {
        SerialPort serialPort;


        public MasterSyncStruct()
        {
            PropertiesSetting propertiesSetting = new PropertiesSetting();

            serialPort = new SerialPort(); //Create a new SerialPort object.
            serialPort.PortName = propertiesSetting.PortName;
            serialPort.BaudRate = propertiesSetting.BaudRate;
            serialPort.DataBits = propertiesSetting.DataBits;


            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
        }


        public void Open()
        {
            serialPort.Open();
            ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(serialPort);
            //ModbusIpMaster modbusIpMaster

            byte slaveID = 1;
            ushort startAddress = 0;
            ushort numOfPoints = 10;
            ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress,numOfPoints);
            
            Console.WriteLine(holding_register);
        }
    }
}
