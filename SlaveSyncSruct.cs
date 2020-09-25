using Modbus.Data;
using Modbus.Device;
using NLog;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Extensions.Enron;


namespace ModbusSyncStructLIb
{
    public class SlaveSyncSruct
    {
        SerialPort serialPort;
        ModbusSlave slave;

        uint[] data = new uint[59];
        byte[] data_byte = new byte[59];
        byte slaveID;

        public SlaveSyncSruct()
        {
            PropertiesSetting propertiesSetting = new PropertiesSetting();

            slaveID = 1;
            serialPort = new SerialPort(propertiesSetting.PortName);
            serialPort.PortName = propertiesSetting.PortName;
            serialPort.BaudRate = propertiesSetting.BaudRate;
            serialPort.DataBits = propertiesSetting.DataBits;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
        }

        public SlaveSyncSruct(byte slaveID, string PortName, int BaudRate,int DataBits)
        {
            this.slaveID = slaveID;
            serialPort = new SerialPort("COM1");
            this.serialPort.PortName = PortName;
            this.serialPort.BaudRate = BaudRate;
            this.serialPort.DataBits = DataBits;

            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
        }

        public void Open()
        {
            try
            {
                serialPort.Open();

                slave = ModbusSerialSlave.CreateRtu(slaveID, serialPort);


                slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                slave.DataStore.DataStoreWrittenTo += new EventHandler<DataStoreEventArgs>(Modbus_DataStoreWriteTo);

                
                slave.DataStore.HoldingRegisters[1] = 222;
                slave.DataStore.HoldingRegisters[2] = 333;

                slave.DataStore.HoldingRegisters[3] = 433;

                slave.DataStore.HoldingRegisters[100] = 433;



                //for (int i=0;i<100;i++)
                //{
                //    slave.DataStore.HoldingRegisters[i] = 0;
                //    Console.WriteLine(i);
                //}

                slave.Listen();
            }
            catch(Exception ex)
            {
                Logger log = LogManager.GetLogger("ModbusSerialSlave");
                LogLevel level = LogLevel.Trace;
                log.Log(level, ex.Message);
            }

        }

        public void close()
        {
            serialPort.Close();
        }


        private void Modbus_DataStoreWriteTo(object sender, Modbus.Data.DataStoreEventArgs e)
        {
            switch (e.ModbusDataType)
            {
                case ModbusDataType.HoldingRegister:
                    for (int i = 0; i < e.Data.B.Count; i++)
                    {
                        //Set AO
                        //e.Data.B[i] already write to
                        //slave.DataStore.HoldingRegisters[e.StartAddress + i + 1];
                        //e.StartAddress starts from 0
                        //You can set AO value to hardware here

                        data[i] = e.Data.B[i];

                    }
                    for (int i = 0; i < data.Length; i++)
                    {
                        data_byte = BitConverter.GetBytes(data[i]);
                    }


                    break;
                case ModbusDataType.Coil:
                    for (int i = 0; i < e.Data.A.Count; i++)
                    {
                        //set DO
                        //e.Data.A[i] already write to
                        //slave.DataStore.CoilDiscretes[e.StartAdd 

                        //ress + i + 1];
                        //e.StartAddress starts from 0
                        //You can set DO value to hardware here
                    }
                    break;
            }
        }

    }
}
