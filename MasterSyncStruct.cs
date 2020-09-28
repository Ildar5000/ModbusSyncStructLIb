using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Security.Cryptography.X509Certificates;
using Modbus.Device;
using NLog;
using System.IO;
using Modbus.Extensions.Enron;
using ModbusSyncStructLIb.DespriptionState;

namespace ModbusSyncStructLIb
{
    [Serializable]
    public class MasterSyncStruct
    {
        SerialPort serialPort;
        byte slaveID;
        PropertiesSetting propertiesSetting;
        ModbusSerialMaster master;
        
        ushort status_slave;


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
                ushort coilAddress = 10;
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
            ushort coilAddress = 10;
            master.WriteMultipleRegisters(slaveID, coilAddress, data);
        }

        //Одиночная пример многопоточной отправка
        public void send_multi_message()
        {
            ushort coilAddress = 1;
            ushort[] data = { 10, 12, 12, 12, 334 };
            master.WriteMultipleRegisters(slaveID, coilAddress, data);
        }

        //отправка пакета о статусе
        public ushort SendRequestforStatusSlave()
        {
            ushort startAddress = 0;
            ushort numOfPoints = 1;
            ushort[] status_slave= master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

            return status_slave[0];
        }

        //отправка пакета с изменением статуса
        public void SendStatusforSlave(ushort status)
        {
            ushort startAddress = 0;
            ushort numOfPoints = 1;

            master.WriteSingleRegister(slaveID, startAddress, status);
        }


        // отправка данных
        public void send_multi_message(MemoryStream stream)
        {
            ushort coilAddress = 10;
            byte[] date = stream.ToArray();
            int count = 50;
            count = (date.Length/2)+1;
            ushort[] date_modbus = new ushort[count];
            
            int needtopacketsend;

            int count_send_packet = 100;
            
            ushort[] sentpacket = new ushort[count_send_packet];
            

            //конвертирует в ushort
            Buffer.BlockCopy(date, 0, date_modbus, 0, (date.Length / 2) + 1);

            status_slave = SendRequestforStatusSlave();


            //есть свободное время у slave
            if (status_slave==SlaveState.have_free_time)
            {
                SendStatusforSlave(SlaveState.havetimetransfer);
                if (date_modbus.Length > count_send_packet)
                {
                    int countneedsend = (date_modbus.Length / count_send_packet) + 1;
                    int k = 0;

                    //кол-во отправок
                    for (int i = 0; i < countneedsend; i++)
                    {
                        /*
                        for (int e = 0; e < sentpacket.Length; e++)
                        {
                            sentpacket[e] = 0;
                        }
                        */

                        //окончание передачи
                        if (countneedsend - 1 == i)
                        {
                            for (int j = i * count_send_packet; j < date_modbus.Length; j++)
                            {
                                sentpacket[k] = date_modbus[j];
                                k++;
                            }
                        }
                        else
                        {
                            for (int j = i * count_send_packet; j < (i + 1) * count_send_packet; j++)
                            {
                                sentpacket[k] = date_modbus[j];
                                k++;
                            }
                        }

                        status_slave = SendRequestforStatusSlave();
                       
                        //если slave свободен то отправляем
                        master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);

                       k = 0;
                    }


                    //после завершение отправки отправить запрос на проверку

                    //
                    SendStatusforSlave(SlaveState.have_free_time);

                }
                else    //в случае если пакет меньше чем ограничения
                {
                    master.WriteMultipleRegisters(slaveID, coilAddress, date_modbus);
                }
            }

            /*  обратная передача
            Buffer.BlockCopy(date_modbus, 0, date_unpack, 0, date.Length);
            byte[] date_unpack = new byte[date.Length];
            Console.WriteLine("yes");
            */

        }

    }
}
