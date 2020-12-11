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
using ModbusSyncStructLIb.DespriptionState;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using StructAllforTest;
using ModbusSyncStructLIb.ControlCheck;
using NLog.Config;
using Modbus.Serial;
using System.Net.Sockets;
using SevenZip;
using System.Diagnostics;
using System.Xml.Serialization;
using ModbusSyncStructLIb.Settings;
using System.Net;

namespace ModbusSyncStructLIb
{
    public class SlaveSyncSruct
    {
        public SerialPort serialPort;

        public SerialPortAdapter SerialPortAdapter;
        public ModbusSlave slave;

        //public ModbusTcpSlave modbusTcp;
        public TcpListener ListenerTCP;

        public double status_bar=0;
        double statusbar_value_repeat = 0;

        public ushort randnumber = 0;

        #region settings
        public int timeRecoveraftercrash = 30 * 1000;

        public bool try_reboot_connection = true;

        int TypeModbus = 0;
        public int deltaTimeCheck = 2000;

        public int stateSlave = SlaveState.have_free_time;


        #endregion
        private static Logger logger;
        
        
        #region События
        public delegate void SignalFormedMetaClassMethod(object struct_which_need_transfer);
        public event SignalFormedMetaClassMethod SignalFormedMetaClass;

        public delegate void SignalFormedMetaClassMethodAll(MetaClassForStructAndtherData metaClassall);
        public event SignalFormedMetaClassMethodAll SignalFormedMetaClassAll;
        #endregion

        public bool have_trasfer=false;
        


        /// <summary>
        /// Кол-во пакетов в одном запросе
        /// </summary>
        int count_send_packet = TableUsedforRegisters.count_packet;

        /// <summary>
        /// Кол-во которое нужно передать
        /// </summary>
        int countDataStruct=0;

        /// <summary>
        /// Кол-во которое нужно передать ushort
        /// </summary>
        int countDataStructUsshort=0;
        /// <summary>
        
        /// Кол-во переданного
        int all_get_packet=0;

        //начало передачи
        bool start_transfer = false;


        /// </summary>
        int countrecivedcount = 0;
        ushort[] receive_packet_data;
        byte[] data_byte_for_processing;
        byte slaveID=1;
        byte[] receivedpacket;
        public MetaClassForStructAndtherData metaClass;

        /// <summary>
        /// Контрольная сумма
        /// </summary>
        private ushort cr16;

        string IP_client;
        int IP_client_port = 502;

        double check_deltatime = 0;


        #region init

        public SlaveSyncSruct()
        {
            try
            {
                stateSlave= SlaveState.have_free_time;
                var loggerconf = new XmlLoggingConfiguration("NLog.config");
                logger = LogManager.GetCurrentClassLogger();

                var path = System.IO.Path.GetFullPath(@"Settingsmodbus.xml");

                if (File.Exists(path) == true)
                {
                    stateSlave = SlaveState.have_free_time;
                    SettingsModbus settings;
                    // десериализация
                    using (FileStream fs = new FileStream("Settingsmodbus.xml", FileMode.OpenOrCreate))
                    {
                        XmlSerializer formatter = new XmlSerializer(typeof(SettingsModbus));
                        settings = (SettingsModbus)formatter.Deserialize(fs);

                    }

                    TypeModbus = settings.typeModbus;
                    try_reboot_connection = settings.try_reboot_connection;
                    deltaTimeCheck = settings.deltatimeManager;
                    timeRecoveraftercrash = settings.tryconnectionaftercrash;

                    if (settings.typeModbus != 2)
                    {
                        serialPort = new SerialPort(settings.ComName);
                        serialPort.BaudRate = settings.BoudRate;
                        serialPort.DataBits = settings.DataBits;
                        serialPort.Parity = (Parity)settings.Party_type_int;
                        check_deltatime = settings.deltatime;

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

                        //serialPort.Parity = Parity.None;
                        //serialPort.StopBits = StopBits.One;

                        serialPort.ReadTimeout = settings.ReadTimeout;
                        serialPort.WriteTimeout = settings.WriteTimeout;
                        check_deltatime = settings.deltatime;
                        slaveID = settings.slaveID;
                    }

                    if (settings.typeModbus == 2)
                    {
                        IP_client = settings.IP_client;
                        IP_client_port = settings.port_IP_client;
                        slaveID = settings.slaveID;
                        check_deltatime = settings.deltatime;
                    }

                }
                else
                {
                    logger.Error("файл с настройками отсутствует");
                }

                receivedpacket = new byte[count_send_packet * 2];
                receive_packet_data = new ushort[count_send_packet];
                //data_byte= new byte[count_send_packet*2];

                metaClass = new MetaClassForStructAndtherData();
            }
            catch(Exception ex)
            {
                stateSlave = SlaveState.haveerror;
                logger.Error(ex);
                logger.Error("Неправильный настройки, пожалуйста проверьте");
            }
            
        }

        public void Open()
        {
            try
            {
                if (TypeModbus == 0)
                {
                    serialPort.Open();
                    SerialPortAdapter = new SerialPortAdapter(serialPort);
                    logger.Info("Создания Modbus RTU");
                    slave = ModbusSerialSlave.CreateRtu(slaveID, SerialPortAdapter);
                    
                    slave.Transport.WriteTimeout = 1000;
                    slave.Transport.ReadTimeout = 1000;
                    
                    logger.Info("Slave подключен");
                    slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                    slave.DataStore.DataStoreWrittenTo += new EventHandler<DataStoreEventArgs>(Modbus_DataStoreWriteTo);
                    slave.DataStore.HoldingRegisters[1] = 0;
                    
                    //logger.Info("Slave состояние" + slave.DataStore.HoldingRegisters[1]);

                    var listenTask = slave.ListenAsync();
                }

                if (TypeModbus == 1)
                {
                    serialPort.Open();
                    SerialPortAdapter = new SerialPortAdapter(serialPort);

                    logger.Info("Создания Modbus ASCII");
                    slave = ModbusSerialSlave.CreateAscii(slaveID, SerialPortAdapter);
                    slave.Transport.WriteTimeout = 1000;
                    slave.Transport.ReadTimeout = 1000;
                    
                    
                    logger.Info("Slave подключен");
                    slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                    slave.DataStore.DataStoreWrittenTo += new EventHandler<DataStoreEventArgs>(Modbus_DataStoreWriteTo);
                    slave.DataStore.HoldingRegisters[1] = 0;
                    
                    //logger.Info("Slave состояние" + slave.DataStore.HoldingRegisters[1]);


                    var listenTask = slave.ListenAsync();
                }

                if (TypeModbus==2)
                {
                    logger.Info("Создания modbus TCP");
                    IPAddress address = IPAddress.Parse(IP_client);
                    
                    IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
                    IPAddress[] addr = ipEntry.AddressList;

                    //IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());
                    ListenerTCP = new TcpListener(IPAddress.Any, IP_client_port);

                    Thread thread = new Thread(ListenerTCP.Start);

                    thread.Start();
                    slave = ModbusTcpSlave.CreateTcp(slaveID, ListenerTCP);
                    logger.Info("Slave подключен");

                    slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                    slave.DataStore.DataStoreWrittenTo += new EventHandler<DataStoreEventArgs>(Modbus_DataStoreWriteTo);
                    
                    slave.DataStore.HoldingRegisters[1] = 0;

                    Task listenTask= slave.ListenAsync();
                    logger.Info("Slave Ожидание");
                }
                
            }
            catch(Exception ex)
            {
                stateSlave = SlaveState.haveerror;
                Logger log = LogManager.GetLogger("ModbusSerialSlave");
                LogLevel level = LogLevel.Error;
                log.Log(level, ex.Message);
            }

        }

        #endregion

        #region stop and end
        public void Close()
        {
            if (slave!=null)
            {
                
                slave.Dispose();
                logger.Warn("Закрытие Slave");

                if (serialPort!=null)
                {
                    serialPort.Close();
                }
                status_bar = 0;
                if (ListenerTCP!=null)
                {
                    ListenerTCP.Stop();
                }
                
            }
        }

        public void StopTransfer()
        {
            if (slave != null && have_trasfer == true)
            {
                slave.DataStore.HoldingRegisters[1] = SlaveState.haveusercanceltransfer;

                logger.Warn("Пользователь отменил передачу");

                Thread.Sleep(400);
                slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                have_trasfer = false;
                status_bar = 0;
            }
        }
        
        #endregion

        #region getters

        /// <summary>
        /// Кол-во которое еобходимо передать
        /// </summary>
        /// <returns></returns>
        public double GetAllPacketNeedTransfer()
        {
            return countDataStruct;
        }

        public double GetTransferPacketNow()
        {
            return all_get_packet;
        }

        public bool GetStartTrasfert()
        {
            return start_transfer;
        }

        #endregion

        private void Modbus_DataStoreWriteTo(object sender, Modbus.Data.DataStoreEventArgs e)
        {
            switch (e.ModbusDataType)
            {
                case ModbusDataType.HoldingRegister:
                    //запросы состояния
                    if (e.Data.B.Count == 1)
                    {
                        //про состояние 

                        if (slave != null)
                        {
                            if (e.StartAddress== TableUsedforRegisters.CR16)
                            {
                                slave.DataStore.HoldingRegisters[TableUsedforRegisters.CR16] = e.Data.B[0];
                                CheckCRC16(e.Data.B[0]);
                            }


                            if (e.StartAddress==TableUsedforRegisters.diagnostik_send)
                            {
                                slave.DataStore.HoldingRegisters[TableUsedforRegisters.diagnostik_send] = e.Data.B[0];
                                ProcessingDiag(e.Data.B[0]);
                            }
                            else
                            {
                                status_bar = 0;
                                all_get_packet = 0;

                                if (e.Data.B[0] == SlaveState.haveusercanceltransfer&&e.StartAddress>TableUsedforRegisters.StateSlaveRegisters)
                                {
                                    logger.Info("Перешел в состояние " + e.Data.B[0] + " Отмена");
                                    slave.DataStore.HoldingRegisters[1] = SlaveState.haveusercanceltransfer;
                                    have_trasfer = false;
                                    ProcessingSingleregx(e.Data.B[0]);
                                    status_bar = 0;


                                }
                                else
                                {
                                    slave.DataStore.HoldingRegisters[1] = SlaveState.havenot_time;

                                    ProcessingSingleregx(e.Data.B[0]);
                                }
                            }  
                        }
                    }

                    if (e.Data.B.Count == 2)
                    {
                        //про состояние 
                        status_bar = 0;
                        all_get_packet = 0;


                        ushort[] tworex=new ushort[2];
                        for (int i = 0; i < e.Data.B.Count; i++)
                        {
                            tworex[i] = e.Data.B[i];
                            //Console.Write(receive_packet_data[i] + " ");
                        }

                        if (slave != null)
                        {
                            ProcessingTworegx(tworex);
                        }
                    }    

                    if (e.Data.B.Count > 2)
                    {
                        have_trasfer = true;
                        if (slave!=null)
                        {
                            if (slave.DataStore.HoldingRegisters[1]!= SlaveState.haveusercanceltransfer)
                            {
                                //logger.Info("Пришел пакет с данными:");
                                for (int i = 0; i < e.Data.B.Count; i++)
                                {
                                    receive_packet_data[i] = e.Data.B[i];
                                }
                                
                                slave.DataStore.HoldingRegisters[1] = SlaveState.havenot_time;

                                ProcessingInfopaket(receive_packet_data);
                            }
                            else
                            {
                                slave.DataStore.HoldingRegisters[TableUsedforRegisters.StateSlaveRegisters] = SlaveState.haveusercanceltransfer;
                                Thread.Sleep(300);

                                //slave.DataStore.HoldingRegisters[TableUsedforRegisters.StateSlaveRegisters] = SlaveState.have_free_time;
                            }

                        }
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

        private void CheckCRC16(ushort v)
        {
                //logger.Info("Состояние slave проверка контрольной суммы");
                // В случае если идет проверка системы
                if (cr16 == v)
                {
                    slave.DataStore.HoldingRegisters[5] = StateCR.haveNotError;
                    logger.Info("(Проверка CR16 состоялась)" + slave.DataStore.HoldingRegisters[5]);
                    slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                    //logger.Info("Сумма совпала " + SlaveState.have_free_time);
                }
        }

        public void ProcessingDiag(ushort v)
        {
             if (randnumber!=v)
            {
                // logger.Trace("Связь присутствует");
                randnumber = v;
            }
            randnumber = v;
        }

        #region Первичная обработка после получения данных
        /// <summary>
        /// обработка статусов
        /// </summary>
        private void ProcessingSingleregx(ushort date)
        {
            if (slave!=null)
            {
                if (slave.DataStore.HoldingRegisters[1] == SlaveState.haveusercanceltransfer)
                {
                    logger.Info("Пользователь отменил посылку");

                    //slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                    UpdateAfteError();
                }

                if (slave.DataStore.HoldingRegisters[1] != SlaveState.havechecktotime)
                {
                    //если данные больше 100, то это кол-ве байт
                    if (date > 70)
                    {
                        countDataStruct = Convert.ToInt32(date);
                        
                        logger.Info("Получен объем данных в байт:" + countDataStruct);
                        int dataushort = (countDataStruct / 2) + 1;
                        //logger.Info("Получен объем данных в ushort:" + dataushort);

                        data_byte_for_processing = new byte[countDataStruct];
                        countDataStructUsshort = (countDataStruct / 2) + 1;

                        //в случии если пакет прервался то обнуляем
                        countrecivedcount = 0;
                    }

                    slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;

                }
            }


        }

        /// <summary>
        /// обработка когда пришло 2 регистра
        /// </summary>
        private void ProcessingTworegx(ushort[] v)
        {
            if (slave != null)
            {
                if (slave.DataStore.HoldingRegisters[1] != SlaveState.havechecktotime)
                {
                    //если данные больше 100, то это кол-ве байт
                    if (v[0] > 70)
                    {
                        int seconddate= Convert.ToInt32(v[1]);
                        //countDataStruct = Convert.ToInt32(v[0]);
                        int back = (v[1] << 16) | v[0];
                        countDataStruct = back;
                        
                        //logger.Info("Получен объем данных в байт:" + countDataStruct);

                        int dataushort = (countDataStruct / 2) + 1;
                        //logger.Info("Получен объем данных в ushort:" + dataushort);

                        data_byte_for_processing = new byte[countDataStruct];
                        countDataStructUsshort = (countDataStruct / 2) + 1;

                        //в случии если пакет прервался то обнуляем
                        countrecivedcount = 0;
                    }
                    slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                }
            }
        }  


        /// <summary>
        /// обработка статусов
        /// </summary>
        private void ProcessingInfopaket(ushort[] date)
        {
            try
            {
                //перводим в массив байт
                Buffer.BlockCopy(date, 0, receivedpacket, 0, receivedpacket.Length);

                countrecivedcount += receivedpacket.Length;
                
                // о статусах
                all_get_packet += date.Length * 2;

                double countpacket = (countDataStruct / 2) / count_send_packet;

                statusbar_value_repeat = 100 / countpacket;

                if (countrecivedcount > countDataStruct)
                {
                    ProcessingInfopaketEndl();
                }
                // начало
                if (countrecivedcount == receivedpacket.Length)
                {
                    ProcessingInfopaketInception();

                    status_bar += statusbar_value_repeat;
                }

                if (countrecivedcount > receivedpacket.Length && countrecivedcount < countDataStruct)
                {
                    ProcessingInfopaketMiddle();
                    status_bar += statusbar_value_repeat;
                }
            }
            catch(Exception ex)
            {
                statusbar_value_repeat = 0;
                logger.Error(ex);
                have_trasfer = false;
            }
            
        }

        #endregion


        #region Обработка инфопакетов
        /// <summary>
        /// Обработка первого пакета
        /// </summary>
        private void ProcessingInfopaketInception()
        {
            //logger.Info("Получен первый инфопакет:");
            for (int i = 0; i < receivedpacket.Length; i++)
            {
                data_byte_for_processing[i] = receivedpacket[i];
            }

            //изменение состояние

            if (slave!=null)
            {
                slave.DataStore.HoldingRegisters[1] = SlaveState.havetimetransfer;
            }
            //logger.Info("Обработан первый инфопакет:");
        }
        
        /// <summary>
        /// Обработка середины
        /// </summary>
        private void ProcessingInfopaketMiddle()
        {
            try
            {
                int delta_countreciveAndSend = countDataStruct - countrecivedcount;
                int delta_start_mid = countrecivedcount - receivedpacket.Length;
                for (int i = 0; i < receivedpacket.Length; i++)
                {
                    data_byte_for_processing[delta_start_mid + i] = receivedpacket[i];
                }

                if (slave!=null)
                {
                    //изменение состояние
                    slave.DataStore.HoldingRegisters[1] = SlaveState.havetimetransfer;
                }

                //logger.Info("Обработан серединный инфопакет");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
        
        /// <summary>
        /// Обработка конца пакета
        /// </summary>
        private void ProcessingInfopaketEndl()
        {
            try
            {               
                int delta_start_mid = countrecivedcount - receivedpacket.Length;
                int delta_countreciveAndSend = Math.Abs(countDataStruct - delta_start_mid);
                
                for (int i = 0; i < delta_countreciveAndSend; i++)
                {
                    data_byte_for_processing[delta_start_mid + i] = receivedpacket[i];
                }

                //logger.Info("Обработан конечный инфопакет");

                //Обнуление переданных пакетов
                //logger.Info("обнуление переданных пакетов");
                countrecivedcount = 0;

                //собираем класс  
                //Сlass_Deserialization(data_byte_for_processing);
                ArchiveCode(data_byte_for_processing);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion


        #region class deserazation

        /// <summary>
        /// Десерилизация класса
        /// </summary>
        private void СlassDeserialization(byte[] date)
        {
            try
            {
                Stream stream = new MemoryStream(date);
                BinaryFormatter formatter = new BinaryFormatter();

                metaClass = (MetaClassForStructAndtherData)formatter.Deserialize(stream);
                
                logger.Info("Сформирован мета-класс");
                //После обработки статус меняется на свободный

                DateTime dataNowSlave = DateTime.Now;

                System.TimeSpan delatatime = dataNowSlave - metaClass.dateTime;
                
                SignalFormedMetaClass?.Invoke(metaClass.struct_which_need_transfer);
                
                if (check_deltatime>=500)
                {
                    if (delatatime.TotalMilliseconds <= check_deltatime)
                    {
                        SignalFormedMetaClass?.Invoke(metaClass.struct_which_need_transfer);   // 2.Вызов события
                    }
                    else
                    {
                        logger.Warn("Данные не актуальные, уточните дельту или данные пришло поздно");
                    }
                }
                else
                {
                    SignalFormedMetaClass?.Invoke(metaClass.struct_which_need_transfer);
                }

                
                if (CheckNameMetaclassobj(metaClass))
                {
                    SignalFormedMetaClassAll?.Invoke(metaClass);
                }
                
                //Контрольная сумма
                Crc16 crc16 = new Crc16();
                byte[] crc16bytes = crc16.ComputeChecksumBytes(date);

                cr16 = crc16.ConverToShort(crc16bytes);
                logger.Info("Сформирована контрольная сумма");
                slave.DataStore.HoldingRegisters[1] = SlaveState.havechecktotime;
                all_get_packet = 0;
                status_bar = 100;
                have_trasfer = false;

                status_bar = 0;
            }
            catch (Exception ex)
            {
                have_trasfer = false;
                logger.Error(ex);
                //have_error_for_deseration();
            }
        }

        private void ArchiveCode(byte[] date)
        {
            try
            {
                MemoryStream stream = new MemoryStream(date);
                BinaryFormatter formatter = new BinaryFormatter();
                

                MemoryStream memory =Decompress(stream,false);
                byte[] class_outdecompress = memory.ToArray();

                logger.Info("Декомпрессия прошла успешна ");
                
                СlassDeserialization(class_outdecompress);
                return;
            }
            catch(Exception ex)
            {
                statusbar_value_repeat = 0;
                have_trasfer = false;
                //have_error_for_deseration();
                logger.Error(ex);
                UpdateAfteError();

            }
        }

        #endregion

        #region other

        private bool CheckNameMetaclassobj(MetaClassForStructAndtherData obj)
        {
            if (obj!=null)
            {
                if (obj.name_file!=null)
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
            return false;
        }

        private void HaveErrorForDeseration()
        {
            if (slave != null)
            {
                slave.DataStore.HoldingRegisters[1] = SlaveState.haveerror;
                logger.Error("Ошибка десеризация");

                ///time reboot
                Thread.Sleep(500);
                UpdateAfteError();
            }
        }

        public void UpdateAfteError()
        {
            if (slave != null)
            {
                Thread.Sleep(300);
                slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                logger.Error("Востановление состояние");
            }
        }

        /// <summary>
        /// Вывод в консоль
        /// </summary>
        /// <param name="date"></param>
        private void WriteByte(byte[] date)
        {
            Console.WriteLine("Вывод пакета байт:");

            for (int i = 0; i < date.Length; i++)
            {
                Console.Write(date[i] + " ");
            }
            Console.WriteLine("");
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
