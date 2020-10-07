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
using NLog.Config;
using ModbusSyncStructLIb.ControlCheck;

namespace ModbusSyncStructLIb
{
    public class MasterSyncStruct
    {
        SerialPort serialPort;
        byte slaveID;
        PropertiesSetting propertiesSetting;
        ModbusSerialMaster master;
        private static Logger logger;
        ushort status_slave;
        Crc16 crc16;

        public MasterSyncStruct()
        {
            var loggerconf = new XmlLoggingConfiguration("NLog.config");
            logger = LogManager.GetCurrentClassLogger();


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
            var loggerconf = new XmlLoggingConfiguration("NLog.config");
            logger = LogManager.GetCurrentClassLogger();

            try
            {
                propertiesSetting = new PropertiesSetting();
                serialPort = new SerialPort();
                serialPort.PortName = text;
                serialPort.BaudRate = propertiesSetting.BaudRate;
                serialPort.DataBits = propertiesSetting.DataBits;
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                Console.WriteLine(ex);
            }
            
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
                logger.Error(ex);
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

        /// <summary>
        /// отправка пакета о статусе
        /// </summary>
        /// <returns></returns>

        public ushort SendRequestforStatusSlave()
        {
            ushort startAddress = 0;
            ushort numOfPoints = 1;
            ushort[] status_slave= master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

            return status_slave[0];
        }

        /// <summary>
        /// Получение значение других регистров
        /// </summary>
        /// <param name="startAddress"></param>
        /// <returns></returns>
        public ushort SendRequestforAnyStatusSlave(ushort startAddress)
        {
            ushort numOfPoints = 1;
            ushort[] status_slave = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

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
            //Console.WriteLine("bytes:");
            logger.Info("bytes:");
            for (int i=0;i<date.Length;i++)
            {
                Console.Write(date[i]+"  ");
            }
        }

        public void write_console(ushort[] date)
        {
            //Console.WriteLine("ushort:");
            logger.Info("ushort:");
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
                status_slave = SendRequestforStatusSlave();
                //есть свободное время у slave для отправки
                if (status_slave == SlaveState.have_free_time)
                {
                    //Console.WriteLine("Статус свободен");
                    logger.Info("Статус свободен:");
                    //SendStatusforSlave(SlaveState.havetimetransfer);

                    //Отправка кол-во байт
                    Sendpaketwithcountbytes(date.Length);

                    logger.Info("Статус свободен:");
                    logger.Info("Отправляем метапкет с кол-вом данных байт" + date.Length);
                    logger.Info("Отправляем метапкет с кол - вом данных ushort" + date_modbus.Length);

                    //Console.WriteLine("Отправляем метапкет с кол-вом данных байт"+ date.Length);
                    //Console.WriteLine("Отправляем метапкет с кол-вом данных ushort" + date_modbus.Length);

                    if (date_modbus.Length > count_send_packet)
                    {
                        //Console.WriteLine("Объем данных больше чем в пакете");
                        logger.Info("Объем данных больше чем в пакете");

                        int countneedsend = (date_modbus.Length / count_send_packet) + 1;
                        int k = 0;
                        //Console.WriteLine("Будет отправлено " + countneedsend + " пакетов");
                        logger.Info("Будет отправлено " + countneedsend + " пакетов");

                        //кол-во отправок
                        for (int i = 0; i < countneedsend; i++)
                        {
                            int counter_reguest_status = 0;

                            //lonsole.WriteLine("Отправляем запрос о статусе");
                            logger.Info("Отправляем запрос о статусе");

                            status_slave = SendRequestforStatusSlave();

                            if (status_slave==SlaveState.havenot_time)
                            {
                                while (counter_reguest_status!=3)
                                {
                                    //Console.WriteLine("Отправляем запрос о статусе, так как был занят на "+i+" попытке");
                                    
                                    logger.Info("Отправляем запрос о статусе, так как был занят на " + i + " попытке");
                                    
                                    Thread.Sleep(1000);
                                    status_slave = SendRequestforStatusSlave();
                                    if (status_slave == SlaveState.have_free_time|| status_slave == SlaveState.havetimetransfer)
                                    {
                                        counter_reguest_status = 3;
                                    }
                                    else
                                    {
                                        counter_reguest_status++;
                                    }
                                    
                                }
                                //если нет свободного статуса в течение 3 попыток идет прекращение передачи
                                if (status_slave == SlaveState.havenot_time)
                                {
                                    //Console.WriteLine("Попытка передачи не удалось на "+i+"передаче");
                                    logger.Warn("Отправляем запрос о статусе, так как был занят на " + i + " попытке");
                                }

                            }

                            if (status_slave == SlaveState.have_free_time|| status_slave == SlaveState.havetimetransfer)
                            {
                                //окончание передачи
                                if (countneedsend-1  == i)
                                {
                                    //Console.WriteLine("Отправка " + i + " пакета");
                                    logger.Trace("Отправка " + i + " пакета");
                                    for (int j = i * count_send_packet; j < date_modbus.Length; j++)
                                    {
                                        sentpacket[k] = date_modbus[j];
                                        k++;
                                    }
                                    //Console.WriteLine("Отправка данных");
                                    logger.Trace("Отправка данных");
                                    write_console(sentpacket);
                                    k=0;
                                    //Console.WriteLine("Отправка данных");
                                    logger.Trace("Отправка данных");


                                    Console.WriteLine("Контрольная сумма");

                                    //Контрольная сумма
                                    crc16 = new Crc16();
                                    byte[] controlsum16 = crc16.ComputeChecksumBytes(date);
                                    
                                    for (int cr1=0; cr1 < controlsum16.Length; cr1++)
                                    {
                                        Console.WriteLine(controlsum16[cr1]);
                                    }

                                    //Отправка контрольной суммы
                                    send_cr16_message(controlsum16);

                                    Console.WriteLine("Cформирован");
                                }
                                else
                                {
                                    //Console.WriteLine("Отправка " + i + " пакета");
                                    logger.Trace("Отправка " + i + " пакета");
                                    for (int j = i * count_send_packet; j < (i + 1) * count_send_packet; j++)
                                    {
                                        sentpacket[k] = date_modbus[j];
                                        k++;
                                    }
                                    //Console.WriteLine("Отправка данных");
                                    logger.Trace("Отправка данных");
                                    write_console(sentpacket);
                                    k = 0;
                                    //Console.WriteLine("Отправка данных");
                                    logger.Trace("Отправка данных");
                                }

                                //status_slave = SendRequestforStatusSlave();

                                //если slave свободен то отправляем
                                master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
                                //Console.WriteLine("Отправлено");
                                
                                logger.Trace("Отправлено");
                                Thread.Sleep(500);
                            }
                            else
                            {
                                //Console.WriteLine("Slave занят: Передача отменена");
                                logger.Trace("Slave занят: Передача отменена");
                                counter_reguest_status = 0;
                            }
                        }
                        //после завершение отправки отправить запрос на проверку
                        SendStatusforSlave(SlaveState.have_free_time);

                    }
                    else    //в случае если пакет меньше чем ограничения
                    {
                        master.WriteMultipleRegisters(slaveID, coilAddress, date_modbus);
                    }
                }
                else  //В случае если не получено данные
                {
                    //Console.WriteLine("Пакет не может передаться, связи с тем, что Slave занят");
                    logger.Warn("Пакет не может передаться, связи с тем, что Slave занят");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                logger.Error(ex);
            }
            

            /*  обратная передача
            Buffer.BlockCopy(date_modbus, 0, date_unpack, 0, date.Length);
            byte[] date_unpack = new byte[date.Length];
            Console.WriteLine("yes");
            */

        }

        /// <summary>
        /// Отправка CR16
        /// </summary>
        /// <param name="date"></param>
        public void send_cr16_message(byte[] date)
        {
            //Отправка данных
            status_slave = SendRequestforStatusSlave();
            

            //Отправляем CR16
            if (status_slave==SlaveState.havechecktotime)
            {
                logger.Info("Отправка контрольной суммы");

                ushort sendCR16=crc16.convertoshort(date);

                ushort coilAddress = 4;
                send_single_message(sendCR16, coilAddress);

                logger.Info("Ожидание ответа");
                Thread.Sleep(100);

                logger.Info("Отправка данных");
                status_slave = SendRequestforAnyStatusSlave(5);


            }

        }

        /// <summary>
        /// Отправка данных
        /// </summary>
        /// <param name="date"></param>
        /// <param name="coilAddress"></param>
        public void send_single_message(ushort date,ushort coilAddress)
        {
            try
            {
                //регистр с контрольной суммой
                ushort value = date;
                master.WriteSingleRegister(slaveID, coilAddress, value);

            }
            catch(Exception ex)
            {
                logger.Error(ex);
            }
        }

        /*
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
            catch (Exception ex)
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
        */
    }
}
