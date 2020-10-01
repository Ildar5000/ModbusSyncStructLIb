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
using System.Threading;

namespace ModbusSyncStructLIb
{
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

            serialPort = new SerialPort(propertiesSetting.PortName); //Create a new SerialPort object.
            serialPort.PortName = propertiesSetting.PortName;
            serialPort.BaudRate = propertiesSetting.BaudRate;
            serialPort.DataBits = propertiesSetting.DataBits;


            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
        }

        public MasterSyncStruct(string text)
        {
            propertiesSetting = new PropertiesSetting();
            serialPort = new SerialPort();
            serialPort.PortName = text;
            serialPort.BaudRate = propertiesSetting.BaudRate;
            serialPort.DataBits = propertiesSetting.DataBits;
        }

        public void Open()
        {
            try
            {
                Console.WriteLine(propertiesSetting.PortName);
                serialPort.Open();
                master = ModbusSerialMaster.CreateRtu(serialPort);
                //ModbusIpMaster modbusIpMaster

                slaveID = 1;
                ushort startAddress = 0;
                ushort numOfPoints = 10;
                ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

                Console.WriteLine(holding_register);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            
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


        #region Примеры
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
            ushort coilAddress = 10;
            ushort[] data = { 10, 12, 12, 12, 334 };
            master.WriteMultipleRegisters(slaveID, coilAddress, data);
        }

        #endregion

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

        //Отправка метопакета с кол-во бит в объекте
        public void Sendpaketwithcountbytes(int count)
        {
            ushort coilAddress = 2;

            ushort sentpacket = Convert.ToUInt16(count);

            master.WriteSingleRegister(slaveID, coilAddress, sentpacket);
        }




        public void write_console(byte[] date)
        {
            Console.WriteLine("bytes:");
            for (int i=0;i<date.Length;i++)
            {
                Console.Write(date[i]+"  ");
            }
        }

        public void write_console(ushort[] date)
        {
            Console.WriteLine("ushort:");
            for (int i = 0; i < date.Length; i++)
            {
                Console.Write(date[i] + "  ");
            }
        }

        // отправка инфоданных
        public void send_multi_message(MemoryStream stream)
        {
            ushort coilAddress = 10;
            byte[] date = stream.ToArray();
            int count = 50;
            count = (date.Length/2)+1;
            ushort[] date_modbus = new ushort[date.Length / 2 + 1];

            int needtopacketsend;

            //Кол-во переднных какналов за 1 запрос
            int count_send_packet = 70;
            
            ushort[] sentpacket = new ushort[count_send_packet];

            write_console(date);
            Console.WriteLine("");
            //конвертирует в ushort
            Buffer.BlockCopy(date, 0, date_modbus, 0, date.Length);

            status_slave = SendRequestforStatusSlave();

            write_console(date_modbus);
            Console.WriteLine("");
            byte[] date_unpack = new byte[date.Length];

            Buffer.BlockCopy(date_modbus, 0, date_unpack, 0, date.Length);
            Console.WriteLine("");
            write_console(date_unpack);
            Console.WriteLine("");
            try
            {
                //есть свободное время у slave
                if (status_slave == SlaveState.have_free_time)
                {
                    Console.WriteLine("Статус свободен");
                    // SendStatusforSlave(SlaveState.havetimetransfer);

                    //Отправка кол-во байт
                    Sendpaketwithcountbytes(date.Length);
                    Console.WriteLine("Отправляем метапкет с кол-вом данных байт"+ date.Length);
                    Console.WriteLine("Отправляем метапкет с кол-вом данных ushort" + date_modbus.Length);

                    if (date_modbus.Length > count_send_packet)
                    {
                        Console.WriteLine("Объем данных больше чем в пакете");
                        int countneedsend = (date_modbus.Length / count_send_packet) + 1;
                        int k = 0;
                        Console.WriteLine("Будет отправлено "+ countneedsend +" пакетов");
                        //кол-во отправок
                        for (int i = 0; i < countneedsend; i++)
                        {
                            //status_slave = SendRequestforStatusSlave();
                            if (status_slave == SlaveState.have_free_time)
                            {
                                //окончание передачи
                                if (countneedsend-1  == i)
                                {
                                    Console.WriteLine("Отправка " + i + " пакета");
                                    for (int j = i * count_send_packet; j < date_modbus.Length; j++)
                                    {
                                        sentpacket[k] = date_modbus[j];
                                        k++;
                                    }
                                    Console.WriteLine("Отправка данных");
                                    write_console(sentpacket);
                                    k=0;
                                    Console.WriteLine("Отправка данных");
                                }
                                else
                                {
                                    Console.WriteLine("Отправка " + i + " пакета");
                                    for (int j = i * count_send_packet; j < (i + 1) * count_send_packet; j++)
                                    {
                                        sentpacket[k] = date_modbus[j];
                                        k++;
                                    }
                                    Console.WriteLine("Отправка данных");
                                    write_console(sentpacket);
                                    k = 0;
                                    Console.WriteLine("Отправка данных");
                                }

                                //status_slave = SendRequestforStatusSlave();

                                //если slave свободен то отправляем
                                master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
                                Console.WriteLine("Отправлено");
                                Thread.Sleep(3000);
                            }
                            else
                            {

                            }
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
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            

            /*  обратная передача
            Buffer.BlockCopy(date_modbus, 0, date_unpack, 0, date.Length);
            byte[] date_unpack = new byte[date.Length];
            Console.WriteLine("yes");
            */

        }

    }
}
