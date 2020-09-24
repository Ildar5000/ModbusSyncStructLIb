﻿using Modbus.Data;
using Modbus.Device;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb
{
    public class SlaveSyncSruct
    {
        SerialPort serialPort;
        ModbusSlave slave;


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
            serialPort.Open();

            slave = ModbusSerialSlave.CreateRtu(slaveID, serialPort);


            slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
            slave.DataStore.DataStoreWrittenTo += new EventHandler<DataStoreEventArgs>(Modbus_DataStoreWriteTo);

            slave.DataStore.HoldingRegisters[1] = 222;
            slave.DataStore.HoldingRegisters[2] = 333;

            slave.DataStore.HoldingRegisters[3] = 433;
            slave.Listen();

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
