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
using Modbus.Serial;
using SevenZip.Compression.LZMA;

using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using SevenZip;
using System.Xml.Serialization;
using ModbusSyncStructLIb.Settings;
using System.Net.Sockets;

namespace ModbusSyncStructLIb
{
    public class MasterSyncStruct
    {
        #region modbus
        SerialPort serialPort;
        SerialPortAdapter SerialPortAdapter;
        ModbusIpMaster masterTCP;
        
        ModbusSerialMaster master;
        #endregion

        public byte slaveID=1;
        
        #region setting
        PropertiesSetting propertiesSetting;
        #endregion

        /// <summary>
        /// Состояние master
        /// </summary>
        public int state_master = 0;


        private static Logger logger;
        ushort status_slave;
        Crc16 crc16;

        int TypeModbus=0;

        string IP_client;
        int IP_client_port = 502;

        public MasterSyncStruct()
        {
            var loggerconf = new XmlLoggingConfiguration("NLog.config");
            logger = LogManager.GetCurrentClassLogger();

            var path = System.IO.Path.GetFullPath(@"Settingsmodbus.xml");

            if (File.Exists(path) == true)
            {
                SettingsModbus settings;
                // десериализация
                using (FileStream fs = new FileStream("Settingsmodbus.xml", FileMode.OpenOrCreate))
                {
                    XmlSerializer formatter = new XmlSerializer(typeof(SettingsModbus));
                    settings = (SettingsModbus)formatter.Deserialize(fs);

                    logger.Info("Объект десериализован");
                }

                serialPort = new SerialPort(settings.ComName);
                serialPort.BaudRate = settings.BoudRate;
                serialPort.DataBits = settings.DataBits;

                switch (settings.Party_type_str)
                {
                    case "Space":
                        serialPort.Parity = Parity.Space;
                        break;
                    case "Even":
                        serialPort.Parity = Parity.Even;
                        break;
                    case "Mark":
                        serialPort.Parity = Parity.Mark;
                        break;
                    case "Odd":
                        serialPort.Parity = Parity.Odd;
                        break;
                    default:
                        //none
                        serialPort.Parity = Parity.None;
                        break;
                }


                switch (settings.StopBits_type_str)
                {
                    case "One":
                        serialPort.StopBits = StopBits.One;
                        break;
                    case "OnePointFive":
                        serialPort.StopBits = StopBits.OnePointFive;
                        break;
                    case "Two":
                        serialPort.StopBits = StopBits.Two;
                        break;
                    default:
                        //none
                        serialPort.StopBits = StopBits.None;
                        break;
                }

                serialPort.ReadTimeout = settings.ReadTimeout;
                serialPort.WriteTimeout = settings.WriteTimeout;
                IP_client = settings.IP_client;
                IP_client_port = settings.port_IP_client;
                slaveID = settings.slaveID;
                TypeModbus = settings.typeModbus;


            }
            else
            {
                logger.Error("Нет файла");
            }

            /*
            propertiesSetting = new PropertiesSetting();
            serialPort = new SerialPort(propertiesSetting.PortName); //Create a new SerialPort object.
            serialPort.PortName = propertiesSetting.PortName;
            serialPort.BaudRate = propertiesSetting.BaudRate;
            serialPort.DataBits = propertiesSetting.DataBits;

            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;

            serialPort.ReadTimeout = 1000;
            serialPort.WriteTimeout = 1000;
            */
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
                TypeModbus = propertiesSetting.TypeComModbus;

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
                serialPort.Open();
                SerialPortAdapter = new SerialPortAdapter(serialPort);

                if (TypeModbus==0)
                {
                    logger.Info("Создания modbus RTU");
                    master = ModbusSerialMaster.CreateRtu(SerialPortAdapter);
                    
                    master.Transport.Retries = 1000;
                    master.Transport.ReadTimeout = 1000;
                    master.Transport.WriteTimeout = 1000;


                    //slaveID = 1;
                    ushort startAddress = 1;
                    ushort numOfPoints = 10;
                    ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
                    Console.WriteLine(holding_register);
                }

                if (TypeModbus == 1)
                {
                    logger.Info("Создания modbus Ascii");
                    master = ModbusSerialMaster.CreateAscii(SerialPortAdapter);
                    
                    master.Transport.Retries = 1000;
                    master.Transport.ReadTimeout = 1000;
                    master.Transport.WriteTimeout = 1000;

                    //slaveID = 1;
                    ushort startAddress = 1;
                    ushort numOfPoints = 10;
                    ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
                    Console.WriteLine(holding_register);
                }

                //ModbusIpMaster modbusIpMaster

                if ((TypeModbus == 2))
                {
                    logger.Info("Создания modbus modbusIp");
                    TcpClient client = new TcpClient(IP_client, IP_client_port);
                    masterTCP = ModbusIpMaster.CreateIp(client);
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                Console.WriteLine(ex);
            }
            
        }

        public ushort[] readHolding()
        {
            ushort startAddress = 1;
            ushort numOfPoints = 10;
            ushort[] holding_register = {0};
            if (TypeModbus==2)
            {
                holding_register = masterTCP.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            }
            else
            {
                holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            }

            return holding_register;
        }

        public void close()
        {
            serialPort.Close();
        }

        /// <summary>
        /// чтение статусе у Slave
        /// </summary>
        /// <returns></returns>

        public ushort SendRequestforStatusSlave()
        {
            ushort startAddress = 1;
            ushort numOfPoints = 1;
            ushort[] status_slave = {0};

            if (master != null)
            {
                status_slave = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            }

            if (masterTCP!=null)
            {
                status_slave = masterTCP.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            }

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
            ushort[] status_slave = { 0 };

            if (master != null)
            {
                status_slave = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            }

            if (masterTCP != null)
            {
                status_slave = masterTCP.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            }

            return status_slave[0];
        }


        /// <summary>
        /// отправка пакета с изменением статуса
        /// </summary>
        public void SendStatusforSlave(ushort status)
        {
            ushort startAddress = 0;

            if (master != null)
            {
                master.WriteSingleRegister(slaveID, startAddress, status);
            }

            if (masterTCP != null)
            {
                masterTCP.WriteSingleRegister(slaveID, startAddress, status);
            }
        }

        //Отправка метопакета с кол-во бит в объекте
        public void Sendpaketwithcountbytes(int count)
        {
            ushort coilAddress = 2;

            ushort sentpacket = Convert.ToUInt16(count);

            if (master != null)
            {
                master.WriteSingleRegister(slaveID, coilAddress, sentpacket);
            }

            if (masterTCP != null)
            {
                masterTCP.WriteSingleRegister(slaveID, coilAddress, sentpacket);
            }           
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

        /// <summary>
        /// отправка инфоданных
        /// </summary>
        public void send_multi_message(MemoryStream stream)
        {
            logger.Info("Изменения структуры и подготовка к передачи");
            //Мастер занят
            state_master = 1;

            ushort coilAddress = 10;
            byte[] date = stream.ToArray();

            int count = 50;
            count = (date.Length/2)+1;
            ushort[] date_modbus = new ushort[date.Length / 2 + 1];

            //Кол-во переднных какналов за 1 запрос
            int count_send_packet = 70;
            
            ushort[] sentpacket = new ushort[count_send_packet];

            write_console(date);
            Console.WriteLine("");
            //конвертирует в ushort

            logger.Info("Преобразование в ushort:Начато");
            Buffer.BlockCopy(date, 0, date_modbus, 0, date.Length);
            logger.Info("Преобразование в ushort:закончено");


            write_console(date_modbus);

            byte[] date_unpack = new byte[date.Length];

            /*
            Buffer.BlockCopy(date_modbus, 0, date_unpack, 0, date.Length);
            Console.WriteLine("");
            write_console(date_unpack);
            Console.WriteLine("");
            */

            try
            {
                logger.Info("Запрос о получении статуса");
                status_slave = SendRequestforStatusSlave();
                logger.Info("Cтатус Slave " + status_slave);

                //есть свободное время у slave для отправки
                if (status_slave == SlaveState.have_free_time)
                {
                    logger.Info("Статус свободен:");
                    
                    //SendStatusforSlave(SlaveState.havetimetransfer);

                    //Отправка кол-во байт
                    Sendpaketwithcountbytes(date.Length);

                    logger.Info("Статус свободен:");
                    logger.Info("Отправляем метапкет с кол-вом данных байт" + date.Length);
                    logger.Info("Отправляем метапкет с кол - вом данных ushort" + date_modbus.Length);

                    //если данные больше чем переданных
                    if (date_modbus.Length > count_send_packet)
                    {
                        morethantransfer(coilAddress,date_modbus, count_send_packet,sentpacket,date);

                    }
                    else    //в случае если пакет меньше чем ограничения
                    {
                        logger.Trace("Передача меньше чем, пакет");
                        logger.Info("Статус свободен:");
                        //SendStatusforSlave(SlaveState.havetimetransfer);
                        //Отправка кол-во байт
                        Sendpaketwithcountbytes(date.Length);
                        logger.Info("Статус свободен:");
                        logger.Info("Отправляем метапкет с кол-вом данных байт" + date.Length);
                        logger.Info("Отправляем метапкет с кол - вом данных ushort" + date_modbus.Length);

                        if (master != null)
                        {
                            master.WriteMultipleRegisters(slaveID, coilAddress, date_modbus);
                        }

                        if (masterTCP != null)
                        {
                            masterTCP.WriteMultipleRegisters(slaveID, coilAddress, date_modbus);
                        }


                        
                    }

                }
                else  //В случае если не получено данные
                {
                    //Console.WriteLine("Пакет не может передаться, связи с тем, что Slave занят");
                    logger.Warn("Пакет не может передаться, связи с тем, что Slave занят");
                    
                    int count_try=0;
                    repeat_try_send(stream, count_try);

                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                logger.Error(ex);
                master.Dispose();
                close();
                Open();
            }
            

            /*  обратная передача
            Buffer.BlockCopy(date_modbus, 0, date_unpack, 0, date.Length);
            byte[] date_unpack = new byte[date.Length];
            Console.WriteLine("yes");
            */

        }
        
        /// <summary>
        /// функция где объем больше чем пакете
        /// </summary>
        private void morethantransfer(ushort coilAddress,ushort[] date_modbus, int count_send_packet, ushort[] sentpacket, byte[] date)
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
                //lonsole.WriteLine("Отправляем запрос о статусе");
                logger.Info("Отправляем запрос о статусе");

                status_slave = SendRequestforStatusSlave();

                if (status_slave == SlaveState.have_free_time || status_slave == SlaveState.havetimetransfer)
                {
                    //окончание передачи
                    if (countneedsend - 1 == i)
                    {
                        end_trasfer_send(i, k, coilAddress, count_send_packet, date_modbus, sentpacket, date);
                    }
                    else
                    {
                        other_trasfer_send(i, k, coilAddress, count_send_packet, date_modbus, sentpacket, date);
                    }
                }
                else
                {
                    logger.Trace("Slave занят: Передача отменена");
                }
            }
        }

        /// <summary>
        /// конечная передача
        /// </summary>
        private void end_trasfer_send(int i,int k, ushort coilAddress, int count_send_packet, ushort[] date_modbus, ushort[] sentpacket, byte[] date)
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
            k = 0;
            //Console.WriteLine("Отправка данных");
            logger.Trace("Отправка данных");

            Console.WriteLine("Контрольная сумма");

            //если slave свободен то отправляем
            if (master != null)
            {
                master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
            }

            if (masterTCP != null)
            {
                masterTCP.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
            }

            //Console.WriteLine("Отправлено");

            logger.Trace("Отправлено");
            Thread.Sleep(50);

            //Контрольная сумма
            crc16 = new Crc16();
            byte[] controlsum16 = crc16.ComputeChecksumBytes(date);

            for (int cr1 = 0; cr1 < controlsum16.Length; cr1++)
            {
                Console.WriteLine(controlsum16[cr1]);
            }

            //Отправка контрольной суммы
            send_cr16_message(controlsum16);

            Console.WriteLine("Cформирован и передан");
        }

        /// <summary>
        /// остальные передачи
        /// </summary>
        /// <param name="i"></param>
        /// <param name="k"></param>
        /// <param name="coilAddress"></param>
        /// <param name="count_send_packet"></param>
        /// <param name="date_modbus"></param>
        /// <param name="sentpacket"></param>
        /// <param name="date"></param>
        private void other_trasfer_send(int i, int k, ushort coilAddress, int count_send_packet, ushort[] date_modbus, ushort[] sentpacket, byte[] date)
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

            //Console.WriteLine("Отправка данных");
            logger.Trace("Отправка данных");

            //если slave свободен то отправляем
            if (master != null)
            {
                master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
            }

            if (masterTCP != null)
            {
                masterTCP.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
            }
        
            //Console.WriteLine("Отправлено");

            logger.Trace("Отправлено");

            state_master = 0;
            Thread.Sleep(50);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream">объект</param>
        /// <param name="count">кол-во попыток</param>
        public void repeat_try_send(MemoryStream stream,int count_try_recurs)
        {
            if (count_try_recurs != 3)
            {
                logger.Info("Попытка передачи " + count_try_recurs);
                //Мастер занят
                state_master = 1;

                ushort coilAddress = 10;
                byte[] date = stream.ToArray();

                int count = 50;
                count = (date.Length / 2) + 1;
                ushort[] date_modbus = new ushort[date.Length / 2 + 1];

                //Кол-во переднных какналов за 1 запрос
                int count_send_packet = 70;

                ushort[] sentpacket = new ushort[count_send_packet];

                write_console(date);
                Console.WriteLine("");
                //конвертирует в ushort

                logger.Info("Преобразование в ushort: Начато");
                Buffer.BlockCopy(date, 0, date_modbus, 0, date.Length);
                logger.Info("Преобразование в ushort: Закончено");


                write_console(date_modbus);

                byte[] date_unpack = new byte[date.Length];

                try
                {
                    logger.Info("Запрос о получении статуса");
                    status_slave = SendRequestforStatusSlave();
                    logger.Info("Cтатус Slave " + status_slave);

                    //есть свободное время у slave для отправки
                    if (status_slave == SlaveState.have_free_time)
                    {
                        logger.Info("Статус свободен:");

                        //SendStatusforSlave(SlaveState.havetimetransfer);

                        //Отправка кол-во байт
                        Sendpaketwithcountbytes(date.Length);

                        logger.Info("Статус свободен:");
                        logger.Info("Отправляем метапкет с кол-вом данных байт: " + date.Length);
                        logger.Info("Отправляем метапкет с кол - вом данных ushort: " + date_modbus.Length);

                        //если данные больше чем переданных
                        if (date_modbus.Length > count_send_packet)
                        {
                            morethantransfer(coilAddress, date_modbus, count_send_packet, sentpacket, date);
                            
                            Console.WriteLine("Cформирован");
                            state_master = 0;
                            logger.Info("Попытка передачи " + count_try_recurs + "Удачное");
                            return;
                        }
                        else    //в случае если пакет меньше чем ограничения
                        {
                            logger.Trace("Передача меньше чем, пакет");
                            logger.Info("Статус свободен:");

                            //SendStatusforSlave(SlaveState.havetimetransfer);
                            //Отправка кол-во байт
                            Sendpaketwithcountbytes(date.Length);

                            logger.Info("Статус свободен:");
                            logger.Info("Отправляем метапкет с кол-вом данных байт: " + date.Length);
                            logger.Info("Отправляем метапкет с кол - вом данных ushort: " + date_modbus.Length);

                            master.WriteMultipleRegisters(slaveID, coilAddress, date_modbus);
                            logger.Info("Попытка передачи " + count_try_recurs + "Удачное");
                            return;
                        }
                        
                    }
                    else  //В случае если не получено данные
                    {

                        //Console.WriteLine("Пакет не может передаться, связи с тем, что Slave занят");
                        logger.Warn("Пакет не может передаться, связи с тем, что Slave занят");

                        Thread.Sleep(1000);
                        
                        if (status_slave==SlaveState.haveerror)
                        {
                            logger.Error("Пакет не может передаться, связи с тем, что Slave возникла ошибка. Передача отменена");
                        }

                        else
                        {
                            count_try_recurs++;
                            repeat_try_send(stream, count_try_recurs);
                        }
                        

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    logger.Error(ex);
                }
            }

            if (count_try_recurs == 3)
            {
                logger.Warn("Пакет не передался");
                return;
            }
        }
        
        /// <summary>
        /// Отправка CR16
        /// </summary>
        /// <param name="date"></param>
        public void send_cr16_message(byte[] date)
        {
            //Отправка данных
            status_slave = SendRequestforStatusSlave();
            logger.Info("Отправка контрольной суммы");

            ushort sendCR16=crc16.convertoshort(date);

            ushort coilAddress = TableUsedforRegisters.CR16;

            send_single_message(sendCR16, coilAddress);

            logger.Info("Отправка контрольной суммы");

            //Мастер свободен
            state_master = 0;

            Thread.Sleep(50);

            //logger.Info("Отправка данных");

            //status_slave = SendRequestforAnyStatusSlave(5);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coilAddress"></param>
        /// <returns></returns>
        public ushort read_single_message(ushort coilAddress)
        {
            ushort[] holding_register = { 0 };
            if (TypeModbus == 2)
            {
                holding_register = masterTCP.ReadHoldingRegisters(slaveID, coilAddress, 0);
            }
            else
            {
                holding_register = master.ReadHoldingRegisters(slaveID, coilAddress, 0);
            }
            return holding_register[0];
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

                if (master != null)
                {
                    master.WriteSingleRegister(slaveID, coilAddress, value);
                }

                if (masterTCP != null)
                {
                    masterTCP.WriteSingleRegister(slaveID, coilAddress, value);
                }

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

        #region 7zip 

        private static Int32 dictionary = 1 << 21; //No dictionary
        private static Int32 posStateBits = 2;
        private static Int32 litContextBits = 3;   // for normal files  // UInt32 litContextBits = 0; // for 32-bit data                                             
        private static Int32 litPosBits = 0;       // UInt32 litPosBits = 2; // for 32-bit data
        private static Int32 algorithm = 2;
        private static Int32 numFastBytes = 128;
        private static bool eos = false;
        private static string mf = "bt4";

        private static CoderPropID[] propIDs =
        {
            CoderPropID.DictionarySize,
            CoderPropID.PosStateBits,
            CoderPropID.LitContextBits,
            CoderPropID.LitPosBits,
            CoderPropID.Algorithm,
            CoderPropID.NumFastBytes,
            CoderPropID.MatchFinder,
            CoderPropID.EndMarker
        };

        private static object[] properties =
        {
            (Int32)(dictionary),
            (Int32)(posStateBits),
            (Int32)(litContextBits),
            (Int32)(litPosBits),
            (Int32)(algorithm),
            (Int32)(numFastBytes),
            mf,
            eos
        };


        public MemoryStream compress(MemoryStream inStream, bool closeInStream)
        {
            inStream.Position = 0;
            Int64 fileSize = inStream.Length;
            MemoryStream outStream = new MemoryStream();

            SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
            encoder.SetCoderProperties(propIDs, properties);
            encoder.WriteCoderProperties(outStream);

            if (BitConverter.IsLittleEndian)
            {
                byte[] LengthHeader = BitConverter.GetBytes(fileSize);
                outStream.Write(LengthHeader, 0, LengthHeader.Length);
            }

            encoder.Code(inStream, outStream, -1, -1, null);

            if (closeInStream)
                inStream.Close();

            return outStream;
        }


        public MemoryStream decompress(MemoryStream inStream, bool closeInStream)
        {
            inStream.Position = 0;
            MemoryStream outStream = new MemoryStream();

            byte[] properties = new byte[5];
            if (inStream.Read(properties, 0, 5) != 5)
                throw (new Exception("input .lzma is too short"));

            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
            decoder.SetDecoderProperties(properties);

            long outSize = 0;

            if (BitConverter.IsLittleEndian)
            {
                for (int i = 0; i < 8; i++)
                {
                    int v = inStream.ReadByte();
                    if (v < 0)
                        throw (new Exception("Can't Read 1"));

                    outSize |= ((long)(byte)v) << (8 * i);
                }
            }

            long compressedSize = inStream.Length - inStream.Position;
            decoder.Code(inStream, outStream, compressedSize, outSize, null);

            if (closeInStream)
                inStream.Close();

            return outStream;
        }

        #endregion
    }
}
