using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
using Modbus.Device;
using NLog;

namespace ModbusSyncStructLIb
{
    public class MasterSyncStruct
    {
        SerialPort serialPort;
        byte slaveID;
        PropertiesSetting propertiesSetting;
        ModbusSerialMaster master;

        public MasterSyncStruct()
        {
            propertiesSetting = new PropertiesSetting();

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
            master = ModbusSerialMaster.CreateRtu(serialPort);
            //ModbusIpMaster modbusIpMaster

            slaveID = 1;
            ushort startAddress = 0;
            ushort numOfPoints = 10;
            ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress,numOfPoints);
            
            Console.WriteLine(holding_register);
        }

        public ushort[] readHolding()
        {
            slaveID = 1;
            ushort startAddress = 0;
            ushort numOfPoints = 10;

            ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

            return holding_register;
        }

        public void close()
        {
            serialPort.Close();
        }

        //Одиночная пример одиночной отправка
        public void send_single_message()
        {
            try
            {
                ushort coilAddress = 1;
                ushort value = 10;
                master.WriteSingleRegister(slaveID, coilAddress, value);
            }
            catch(Exception ex)
            {
                Logger log = LogManager.GetLogger("ModbusSerialMaster");
                LogLevel level = LogLevel.Trace;
                log.Log(level, ex.Message);
            }
        }

        public void send_single_message(ushort value, ushort coilAddress)
        {
            try
            {
                //ushort coilAddress = 1;
                //ushort value = value;
                master.WriteSingleRegister(slaveID, coilAddress, value);
            }
            catch (Exception ex)
            {
                Logger log = LogManager.GetLogger("ModbusSerialMaster");
                LogLevel level = LogLevel.Trace;
                log.Log(level, ex.Message);
            }
        }

        public void send_multi_message(ushort[] data)
        {
            ushort coilAddress = 1;
            master.WriteMultipleRegisters(slaveID, coilAddress, data);
        }

        //Одиночная пример многопоточной отправка
        public void send_multi_message()
        {
            ushort coilAddress = 1;
            ushort[] data = { 10, 12, 12, 12, 334 };
            master.WriteMultipleRegisters(slaveID, coilAddress, data);
        }


    }
}
