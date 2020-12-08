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
using Modbus.Message;

namespace ModbusSyncStructLIb
{
    public class MasterSyncStruct
    {
        #region modbus serial
        SerialPort serialPort;
        SerialPortAdapter SerialPortAdapter;
        public ModbusMaster master;
        #endregion

        #region modbus tcp
        string IP_client;
        int IP_client_port = 502;
        #endregion

        //Common settings
        int TypeModbus = 0;
        public byte slaveID=1;

        public bool try_reboot_connection = true;

        #region setting
        PropertiesSetting propertiesSetting;
        public int deltaTimeCheck = 2000;

        public int timeRecoveraftercrash = 30 * 1000;

        #endregion

        /// <summary>
        /// Состояние master
        /// </summary>
        public int state_master = 0;
        Crc16 crc16;
        ushort status_slave;

        //log
        private static Logger logger;

        /// <summary>
        /// Статус отмены пользователем
        /// </summary>
        public bool stoptransfer_signal = false;

        public bool havetrasfer = false;

        #region статусы

        /// <summary>
        /// Измерение временени в тиках
        /// </summary>
        public long ellapledTicks = DateTime.Now.Ticks;

        public TimeSpan elapsedSpan;

        /// <summary>
        /// Статус процесса
        /// </summary>
        public double status_bar = 0;
        double status_bar_temp = 0;
        #endregion

        #region date
        byte[] date;
        ushort[] sentpacket;

        double alltranferendpacket=0;

        #endregion



        #region init

        public MasterSyncStruct()
        {
            var loggerconf = new XmlLoggingConfiguration("NLog.config");
            logger = LogManager.GetCurrentClassLogger();

            var path = System.IO.Path.GetFullPath(@"Settingsmodbus.xml");

            try
            {
                if (File.Exists(path) == true)
                {
                    SettingsModbus settings;
                    // десериализация
                    using (FileStream fs = new FileStream("Settingsmodbus.xml", FileMode.OpenOrCreate))
                    {
                        XmlSerializer formatter = new XmlSerializer(typeof(SettingsModbus));
                        settings = (SettingsModbus)formatter.Deserialize(fs);

                        logger.Info("Настройки найдены");
                    }
                    TypeModbus = settings.typeModbus;
                    try_reboot_connection = settings.try_reboot_connection;

                    deltaTimeCheck = settings.deltatimeManager;
                    timeRecoveraftercrash = settings.tryconnectionaftercrash;
                    slaveID = settings.slaveID;

                    if (settings.typeModbus != 2)
                    {
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


                    }
                    if (settings.typeModbus == 2)
                    {

                        IP_client = settings.IP_client;
                        IP_client_port = settings.port_IP_client;
                        slaveID = settings.slaveID;
                    }
                }
                else
                {
                    logger.Error("Нет файла");
                }
            }
            catch (Exception ex)
            {
                state_master = SlaveState.haveerror;
                logger.Error(ex);
            }

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
            state_master = 0;
            try
            {
                if (TypeModbus==0)
                {
                    serialPort.Open();
                    SerialPortAdapter = new SerialPortAdapter(serialPort);

                    logger.Info("Создания modbus RTU");
                    master = ModbusSerialMaster.CreateRtu(SerialPortAdapter);
                    
                    //slaveID = 1;
                    ushort startAddress = 1;
                    ushort numOfPoints = 10;

                    ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);

                    var transport = master.Transport;
                }

                if (TypeModbus == 1)
                {
                    serialPort.Open();
                    SerialPortAdapter = new SerialPortAdapter(serialPort);

                    logger.Info("Создания modbus Ascii");
                    master = ModbusSerialMaster.CreateAscii(SerialPortAdapter);
                    
                    master.Transport.Retries = 1000;
                    master.Transport.ReadTimeout = 1000;
                    master.Transport.WriteTimeout = 1000;

                    //slaveID = 1;
                    ushort startAddress = 1;
                    ushort numOfPoints = 10;
                    ushort[] holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
                    //Console.WriteLine(holding_register);
                }

                //ModbusIpMaster modbusIpMaster

                if ((TypeModbus == 2))
                {
                    logger.Info("Создания modbus modbusIp");
                    TcpClient client = new TcpClient();
                    client.Connect(IP_client, IP_client_port);
                    master = ModbusIpMaster.CreateIp(client);
                }
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                logger.Error("Неправильный настройки, пожалуйста проверьте");
                state_master = SlaveState.haveerror;

                state_master = SlaveState.have_free_time;

                return;
            }
            
        }

        #endregion

        #region getters
        /// <summary>
        /// Кол-во которое необходимо передать
        /// </summary>
        /// <returns></returns>
        public double GetDataTrasfer()
        {
            if (date!=null)
            {
                return date.Length;
            }
            else
            {
                return 0;
            }
        }
        
        /// <summary>
        /// Кол-во которое передали
        /// </summary>
        /// <returns></returns>
        public double GetDataTrasferNow()
        {
            return alltranferendpacket;
        }

        #endregion

        #region stop and endl
        public void Close()
        {
            try
            {
                StopTransfer();
                if (master != null)
                {
                    logger.Warn("Остановлен мастер");
                    master.Dispose();
                   
                }

                if (serialPort!=null)
                {
                    logger.Warn("Закрыт порт");
                    serialPort.Close();
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void StopTransfer()
        {
            stoptransfer_signal = true;
            state_master = SlaveState.haveerror;
            Thread.Sleep(100);
        }
        #endregion



        #region readslave
        
        /// <summary>
        /// чтение статусе у Slave
        /// </summary>
        /// <returns></returns>
        public ushort SendRequestForStatusSlave()
        {
            ushort startAddress = 0;
            ushort numOfPoints = 10;
            ushort[] status_slave = {0};

            if (master != null)
            {
                status_slave = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            }
            else
            {
                return SlaveState.haveerror;
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

            return status_slave[0];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="coilAddress"></param>
        /// <returns></returns>
        public ushort ReadSinglMessage(ushort coilAddress)
        {
            ushort[] holding_register = { 0 };
            holding_register = master.ReadHoldingRegisters(slaveID, coilAddress, 1);
            return holding_register[0];
        }

        /// <summary>
        /// Отправка данных
        /// </summary>
        /// <param name="date"></param>
        /// <param name="coilAddress"></param>
        public void SendSingleMessage(ushort date, ushort coilAddress)
        {
            try
            {
                //регистр с контрольной суммой
                ushort value = date;

                if (master != null)
                {
                    master.WriteSingleRegister(slaveID, coilAddress, value);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }


        public ushort[] ReadHolding()
        {
            ushort startAddress = 1;
            ushort numOfPoints = 10;
            ushort[] holding_register = { 0 };

            holding_register = master.ReadHoldingRegisters(slaveID, startAddress, numOfPoints);
            return holding_register;
        }

        #endregion


        #region writeslave


        /// <summary>
        /// отправка пакета с изменением статуса
        /// </summary>
        public void SendStatusforSlave(ushort status)
        {
            ushort startAddress = 1;

            if (master != null)
            {
                master.WriteSingleRegister(slaveID, startAddress, status);
            }

        }



        #endregion



        #region отправка инфо о кол-во бит


        //Отправка метопакета с кол-во бит в объекте
        public void SendPaketWithCountBytes(int count)
        {
            ushort coilAddress = TableUsedforRegisters.SendDate;

            if (count<2147483647)
            {
                //2147483647
                //2147483647
                if (count > 60000)
                {
                    ushort[] sentpacket_second1 = new ushort[2];

                    sentpacket_second1[0] = (ushort)count;
                    sentpacket_second1[1] = (ushort)(count >> 16);
                    SendBigDataForMaster(coilAddress, sentpacket_second1);
                }
                else
                {
                    ushort sentpacket = Convert.ToUInt16(count);
                    SendDataForMaster(coilAddress, sentpacket);
                }
            }
            else
            {
                logger.Warn("Слишком большой файл");
            }

         
        }
        
        /// <summary>
        /// Отправить данные
        /// </summary>
        /// <param name="coilAddress">с какого начинаетс</param>
        /// <param name="sentpacket">кол-во байт</param>
        private void SendDataForMaster(ushort coilAddress,ushort sentpacket)
        {
            if (master != null)
            {
                master.WriteSingleRegister(slaveID, coilAddress, sentpacket);
            }
        }

        private void SendBigDataForMaster(ushort coilAddress, ushort[] sentpacket)
        {
            if (master != null)
            {
                master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
            }
        }
        #endregion


        #region sendfilesandStruct

        /// <summary>
        /// отправка инфоданных
        /// </summary>
        public void SendMultiMessage(MemoryStream stream)
        {
            status_bar = 0;
            havetrasfer = true;
            stoptransfer_signal = false;
            state_master = 0;
            //logger.Info("Изменения структуры и подготовка к передачи");
            //Мастер занят
            
            //state_master = 1;

            ushort coilAddress = 10;
            date = stream.ToArray();

            int count = 50;
            count = (date.Length/2)+1;
            ushort[] date_modbus = new ushort[date.Length / 2 + 1];

            //Кол-во переднных какналов за 1 запрос
            int count_send_packet = TableUsedforRegisters.count_packet;
            
            //передача за 1 раз
            sentpacket = new ushort[count_send_packet];

            //конвертирует в ushort
            alltranferendpacket = 0;

            Buffer.BlockCopy(date, 0, date_modbus, 0, date.Length);

            try
            {
                ellapledTicks = DateTime.Now.Ticks;
  
                status_slave = SendRequestForStatusSlave();

                //есть свободное время у slave для отправки
                if (status_slave == SlaveState.have_free_time)
                {
                    //Отправка кол-во байт
                    SendPaketWithCountBytes(date.Length);
                    logger.Info("Отправляем метапкет с кол-вом данных байт" + date.Length);
                    //logger.Info("Отправляем метапкет с кол - вом данных ushort" + date_modbus.Length);
                    
                    ellapledTicks = DateTime.Now.Ticks;
                    //если данные больше чем переданных
                    if (date_modbus.Length > count_send_packet)
                    {
                        MoreThanTransfer(coilAddress,date_modbus, count_send_packet);
                    }
                    else    //в случае если пакет меньше чем ограничения
                    {
                        logger.Trace("Передача меньше чем, пакет");
                        if (master != null)
                        {
                            master.WriteMultipleRegisters(slaveID, coilAddress, date_modbus);
                        }
                    }

                    if (stoptransfer_signal==true)
                    {
                        logger.Info("Передача отменена");
                        //Thread.Sleep(500);
                        //stoptransfer_signal = false;
                        havetrasfer = false;
                        state_master = 0;
                        return;
                    }
                    else
                    {
                        ellapledTicks = DateTime.Now.Ticks - ellapledTicks;
                        elapsedSpan = new TimeSpan(ellapledTicks);

                        logger.Info("Передан за " + Math.Round(elapsedSpan.TotalSeconds,1) + "Секунд");
                        stoptransfer_signal = false;
                    }

                    havetrasfer = false;
                    

                }
                else  //В случае если не получено данные
                {
                    //Console.WriteLine("Пакет не может передаться, связи с тем, что Slave занят");
                    logger.Warn("Пакет не может передаться, связи с тем, что Slave занят");
                    int count_try=0;
                    RepeatTrySend(stream, count_try);
                }
            }
            catch(Exception ex)
            {
                stoptransfer_signal = true;
                havetrasfer = false;
                //Console.WriteLine(ex);
                logger.Error("Не удалось отправить данные");
                logger.Error(ex);
                state_master = SlaveState.haveerror;
                //master.Dispose();
                //close();
                //Open();
            }
        }
        
        /// <summary>
        /// функция где объем больше чем пакете
        /// </summary>
        private void MoreThanTransfer(ushort coilAddress,ushort[] date_modbus, int count_send_packet)
        {
            int countneedsend = (date_modbus.Length / count_send_packet) + 1;
            
            //logger.Info("Будет отправлено " + countneedsend + " пакетов");

            status_bar_temp = 100 / Convert.ToDouble(countneedsend);

            //кол-во пакетов
            for (int i = 0; i < countneedsend; i++)
            {
                
                //если пользователь отменил передачу
                if (stoptransfer_signal == true)
                {
                    logger.Warn("Пользователь отменил передачу");
                    status_bar = 0;
                    SendSingleMessage(SlaveState.haveusercanceltransfer, TableUsedforRegisters.StateSlaveRegisters);
                    //SendSingleMessage(SlaveState.have_free_time, TableUsedforRegisters.StateSlaveRegisters);
                    return;
                }

                status_slave = SendRequestForStatusSlave();

                if   (status_slave == SlaveState.haveusercanceltransfer)
                {
                    logger.Warn("Пользователь отменил передачу у Slave");
                    stoptransfer_signal = true;

                    status_bar = 0;
                    state_master = SlaveState.haveerror;
                    return;
                }
                status_slave = SendRequestForStatusSlave();
                if (status_slave == SlaveState.have_free_time || status_slave == SlaveState.havetimetransfer)
                {
                    SendTypePacket(coilAddress, date_modbus, i, countneedsend);
                }
                else
                {
                    if (stoptransfer_signal == true)
                    {
                        return;
                    }

                    int count= 0;
                    MoreThanTransferRepet(coilAddress, date_modbus, i, count, countneedsend);
                    if (state_master== SlaveState.haveerror)
                    {
                        break;
                    }

                }
            }
        }

        private void SendTypePacket(ushort coilAddress, ushort[] date_modbus,int i,int countneedsend)
        {
                //окончание передачи
                if (countneedsend - 1 == i)
                {
                    status_bar += status_bar_temp;
                    EndPacketTrasferSend(i, coilAddress, date_modbus, date);
                }
                else
                {
                    status_bar += status_bar_temp;
                    OtherPacketTrasferSend(i, coilAddress, date_modbus, date);
                }

                // о статусе
                alltranferendpacket += sentpacket.Length * 2;
        }

        /// <summary>
        /// конечная передача
        /// </summary>
        /// <param name="i"></param>
        /// <param name="k"></param>
        /// <param name="coilAddress"></param>
        /// <param name="count_send_packet"></param>
        /// <param name="date_modbus"></param>
        /// <param name="sentpacket"></param>
        /// <param name="date"></param>
        private void EndPacketTrasferSend(int i, ushort coilAddress, ushort[] date_modbus, byte[] date)
        {
            int k = 0;

            //logger.Trace("Отправка " + i + " пакета");

            int count_send_packet = TableUsedforRegisters.count_packet;
            sentpacket = new ushort[TableUsedforRegisters.count_packet];
            for (int j = i * count_send_packet; j < date_modbus.Length; j++)
            {
                sentpacket[k] = date_modbus[j];
                k++;
            }
            //если slave свободен то отправляем
            if (master != null)
            {
                master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
            }
            Thread.Sleep(50);

            //Контрольная сумма
            crc16 = new Crc16();
            byte[] controlsum16 = crc16.ComputeChecksumBytes(date);
            state_master = 0;
            //Отправка контрольной суммы
            SendCr16Message(controlsum16);

            havetrasfer = false;

            status_bar = 100;
            status_bar = 0;
            logger.Info("Объект передан и сформирован у Slave");
            logger.Info("Передача окончена");

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
        private void OtherPacketTrasferSend(int i, ushort coilAddress, ushort[] date_modbus, byte[] date)
        {
            int k = 0;
            sentpacket = new ushort[TableUsedforRegisters.count_packet];
            int count_send_packet = TableUsedforRegisters.count_packet;
            //logger.Trace("Отправка " + i + " пакета");
            for (int j = i * count_send_packet; j < (i + 1) * count_send_packet; j++)
            {
                sentpacket[k] = date_modbus[j];
                k++;
            }

            status_slave = SendRequestForStatusSlave();

            if (status_slave == SlaveState.haveusercanceltransfer)
            {
                logger.Warn("Пользователь отменил передачу у Slave");
                havetrasfer = false;
                stoptransfer_signal = true;
                status_bar = 0;
                return;
            }
            //если slave свободен то отправляем
            if (master != null)
            {
                master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
            }
            state_master = 0;
            Thread.Sleep(10);
        }
        #endregion


        #region trySend
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream">объект</param>
        /// <param name="count">кол-во попыток</param>
        public void RepeatTrySend(MemoryStream stream,int count_try_recurs) 
        {
            //если пользователь отменил передачу
            if (stoptransfer_signal == true)
            {
                logger.Info("Пользователь отменил передачу");
                return;
            }

            status_slave = SendRequestForStatusSlave();

            if (status_slave == SlaveState.haveusercanceltransfer)
            {
                logger.Warn("Пользователь отменил передачу у Slave");
                stoptransfer_signal = true;
                status_bar = 0;
                state_master = SlaveState.haveerror;
                return;
            }

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

                //logger.Info("Преобразование в ushort: Начато");
                Buffer.BlockCopy(date, 0, date_modbus, 0, date.Length);
                //logger.Info("Преобразование в ushort: Закончено");

                byte[] date_unpack = new byte[date.Length];

                try
                {
                    status_slave = SendRequestForStatusSlave();
                    //есть свободное время у slave для отправки
                    if (status_slave == SlaveState.have_free_time)
                    {
                        //Отправка кол-во байт
                        SendPaketWithCountBytes(date.Length);

                        //logger.Info("Статус свободен:");
                        //logger.Info("Отправляем метапкет с кол-вом данных байт: " + date.Length);
                        //logger.Info("Отправляем метапкет с кол - вом данных ushort: " + date_modbus.Length);

                        //если данные больше чем переданных
                        if (date_modbus.Length > count_send_packet)
                        {
                            MoreThanTransfer(coilAddress, date_modbus, count_send_packet);

                            state_master = 0;
                            logger.Info("Попытка передачи " + count_try_recurs + "Удачное");
                            return;
                        }
                        else    //в случае если пакет меньше чем ограничения
                        {
                            //logger.Trace("Передача меньше чем, пакет");
                            //logger.Info("Статус свободен:");

                            //SendStatusforSlave(SlaveState.havetimetransfer);
                            //Отправка кол-во байт
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
                            RepeatTrySend(stream, count_try_recurs);
                        }
                        

                    }
                }
                catch (Exception ex)
                {
                    stoptransfer_signal = true;
                    havetrasfer = false;
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

        private void MoreThanTransferRepet(ushort coilAddress, ushort[] date_modbus, int i, int count, int countneedsend)
        {
            Thread.Sleep(100);

            if (stoptransfer_signal == true)
            {
                //stoptransfer_signal = false;
                return;
            }

            if (status_slave == SlaveState.haveusercanceltransfer)
            {
                logger.Warn("Пользователь отменил передачу у Slave");
                stoptransfer_signal = true;
                status_bar = 0;
                state_master = SlaveState.haveerror;
                return;
            }

            if (count == 3)
            {
                logger.Error("Ошибка");
                state_master = SlaveState.haveerror;
                return;
            }
            else
            {
                status_slave = SendRequestForStatusSlave();
                logger.Info("Попытка номер " + count);

                if (status_slave == SlaveState.have_free_time || status_slave == SlaveState.havetimetransfer)
                {
                    SendTypePacket(coilAddress, date_modbus, i, countneedsend);
                }
                else
                {
                    MoreThanTransferRepet(coilAddress, date_modbus, i, count, countneedsend);
                }
                count++;
            }
        }


        #endregion



        /// <summary>
        /// Отправка CR16
        /// </summary>
        /// <param name="date"></param>
        public void SendCr16Message(byte[] date)
        {
            //Отправка данных
            status_slave = SendRequestForStatusSlave();
            logger.Info("Отправка контрольной суммы");

            ushort sendCR16=crc16.ConverToShort(date);

            ushort coilAddress = TableUsedforRegisters.CR16;

            SendSingleMessage(sendCR16, coilAddress);

            //Мастер свободен
            state_master = 0;
            havetrasfer = false;
            Thread.Sleep(50);
        }


        #region вывод в консоль список байтов

        public void WriteConsole(byte[] date)
        {
            //Console.WriteLine("bytes:");
            logger.Info("bytes:");
            for (int i = 0; i < date.Length; i++)
            {
                Console.Write(date[i] + "  ");
            }
        }

        public void WriteConsole(ushort[] date)
        {
            //Console.WriteLine("ushort:");
            logger.Info("ushort:");
            for (int i = 0; i < date.Length; i++)
            {
                Console.Write(date[i] + "  ");
            }
        }

        #endregion


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


        public MemoryStream Compress(MemoryStream inStream, bool closeInStream)
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


        public MemoryStream Decompress(MemoryStream inStream, bool closeInStream)
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
