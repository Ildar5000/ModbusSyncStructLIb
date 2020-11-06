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

        public ModbusTcpSlave modbusTcp;
        public TcpListener ListenerTCP;

        int TypeModbus=0;

        private static Logger logger;
        //События
        #region События
        public delegate void SignalFormedMetaClassMethod(object struct_which_need_transfer);
        public event SignalFormedMetaClassMethod SignalFormedMetaClass;

        #endregion

        /// <summary>
        /// Кол-во пакетов в одном запросе
        /// </summary>
        int count_send_packet = 70;

        /// <summary>
        /// Кол-во которое нужно передать
        /// </summary>
        int countDataStruct;

        /// <summary>
        /// Кол-во которое нужно передать ushort
        /// </summary>
        int countDataStructUsshort; 
        /// <summary>
        /// Кол-во переданного
        
        /// </summary>
        int countrecivedcount = 0;
        ushort[] receive_packet_data;
        byte[] data_byte_for_processing;
        byte slaveID=1;
        byte[] receivedpacket;
        public MetaClassForStructandtherdata metaClass;

        /// <summary>
        /// Контрольная сумма
        /// </summary>
        private ushort cr16;

        string IP_client;
        int IP_client_port = 502;

        public SlaveSyncSruct()
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

                }

                TypeModbus = settings.typeModbus;

                if (settings.typeModbus!=2)
                {
                    serialPort = new SerialPort(settings.ComName);
                    serialPort.BaudRate = settings.BoudRate;
                    serialPort.DataBits = settings.DataBits;
                    serialPort.Parity = (Parity)settings.Party_type_int;

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

                    slaveID = settings.slaveID;
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


            /*
            PropertiesSetting propertiesSetting = new PropertiesSetting();
            slaveID = 1;
            serialPort = new SerialPort(propertiesSetting.PortName);
            
            serialPort.PortName = propertiesSetting.PortName;
            serialPort.BaudRate = propertiesSetting.BaudRate;
            serialPort.DataBits = propertiesSetting.DataBits;
            TypeModbus = propertiesSetting.TypeComModbus;

            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;

            serialPort.ReadTimeout = 1000;
            serialPort.WriteTimeout = 1000;
            */

            receivedpacket = new byte[count_send_packet*2];
            receive_packet_data = new ushort[count_send_packet];
            //data_byte= new byte[count_send_packet*2];

            metaClass = new MetaClassForStructandtherdata();
        }

        public void Open()
        {
            try
            {
                if (TypeModbus == 0)
                {
                    serialPort.Open();
                    SerialPortAdapter = new SerialPortAdapter(serialPort);


                    logger.Info("Создания modbus RTU");
                    slave = ModbusSerialSlave.CreateRtu(slaveID, SerialPortAdapter);
                    
                    slave.Transport.WriteTimeout = 1000;
                    slave.Transport.ReadTimeout = 1000;
                    
                    logger.Info("Slave подключен");
                    slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                    slave.DataStore.DataStoreWrittenTo += new EventHandler<DataStoreEventArgs>(Modbus_DataStoreWriteTo);
                    slave.DataStore.HoldingRegisters[1] = 0;
                    
                    logger.Info("Slave состояние" + slave.DataStore.HoldingRegisters[1]);

                    var listenTask = slave.ListenAsync();
                }

                if (TypeModbus == 1)
                {
                    serialPort.Open();
                    SerialPortAdapter = new SerialPortAdapter(serialPort);

                    logger.Info("Создания modbus Ascii");
                    slave = ModbusSerialSlave.CreateAscii(slaveID, SerialPortAdapter);
                    slave.Transport.WriteTimeout = 1000;
                    slave.Transport.ReadTimeout = 1000;
                    
                    
                    logger.Info("Slave подключен");
                    slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                    slave.DataStore.DataStoreWrittenTo += new EventHandler<DataStoreEventArgs>(Modbus_DataStoreWriteTo);
                    slave.DataStore.HoldingRegisters[1] = 0;
                    logger.Info("Slave состояние" + slave.DataStore.HoldingRegisters[1]);


                    var listenTask = slave.ListenAsync();
                }

                if (TypeModbus==2)
                {
                    logger.Info("Создания modbus TCP");
                    IPAddress address = IPAddress.Parse(IP_client);
                    ListenerTCP = new TcpListener(address, IP_client_port);
                    Thread thread = new Thread(ListenerTCP.Start);
                    
                    thread.Start();

                    logger.Info("Slave подключен");

                    modbusTcp = ModbusTcpSlave.CreateTcp(slaveID, ListenerTCP);

                    modbusTcp.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                    modbusTcp.DataStore.DataStoreWrittenTo += new EventHandler<DataStoreEventArgs>(Modbus_DataStoreWriteTo);

                    Task listenTask=  modbusTcp.ListenAsync();
                    logger.Info("Slave Ожидание");
                  
                }
                
            }
            catch(Exception ex)
            {
                Logger log = LogManager.GetLogger("ModbusSerialSlave");
                LogLevel level = LogLevel.Error;
                log.Log(level, ex.Message);
            }

        }

        public void close()
        {
            if (modbusTcp!=null)
            {
                logger.Warn("Закрытие Slave");
                modbusTcp.Dispose();
            }
            if (slave!=null)
            {
                
                slave.Dispose();
                logger.Warn("Закрытие Slave");
                serialPort.Close();
            }
        }
      
        private void Modbus_DataStoreWriteTo(object sender, Modbus.Data.DataStoreEventArgs e)
        {
            switch (e.ModbusDataType)
            {
                case ModbusDataType.HoldingRegister:
                    //запросы состояния
                    if (e.Data.B.Count == 1)
                    {
                        if (slave != null)
                        {
                            logger.Info("Перешел в состояние " + e.Data.B[0] + " обработка");
                            slave.DataStore.HoldingRegisters[1] = SlaveState.havenot_time;

                            processing_singleregx(e.Data.B[0]);
                            //Console.WriteLine("Пришла команда на обработку"+e.Data.B[0]);
                            logger.Info("Пришла команда на обработку" + e.Data.B[0]);
                        }

                        if (modbusTcp != null)
                        {
                            logger.Info("Перешел в состояние " + e.Data.B[0] + " обработка");
                            modbusTcp.DataStore.HoldingRegisters[1] = SlaveState.havenot_time;

                            processing_singleregx(e.Data.B[0]);
                            //Console.WriteLine("Пришла команда на обработку"+e.Data.B[0]);
                            logger.Info("Пришла команда на обработку" + e.Data.B[0]);
                        }

                    }

                    if (e.Data.B.Count > 1)
                    {

                        if (modbusTcp != null)
                        {
                            //Console.WriteLine("Пришел пакет с данными:");
                            logger.Info("Пришел пакет с данными:");
                            for (int i = 0; i < e.Data.B.Count; i++)
                            {
                                receive_packet_data[i] = e.Data.B[i];
                                Console.Write(receive_packet_data[i] + " ");
                            }
                            Console.WriteLine("");
                            logger.Info("Перешел в состояние " + e.Data.B[0] + " обработка");
                            modbusTcp.DataStore.HoldingRegisters[1] = SlaveState.havenot_time;

                            //Console.WriteLine("Обработка пакета");
                            logger.Info("Обработка пакета");
                            processing_infopaket(receive_packet_data);
                        }
                        if (slave!=null)
                        {
                            //Console.WriteLine("Пришел пакет с данными:");
                            logger.Info("Пришел пакет с данными:");
                            for (int i = 0; i < e.Data.B.Count; i++)
                            {
                                receive_packet_data[i] = e.Data.B[i];
                                Console.Write(receive_packet_data[i] + " ");
                            }
                            Console.WriteLine("");
                            logger.Info("Перешел в состояние " + e.Data.B[0] + " обработка");
                            slave.DataStore.HoldingRegisters[1] = SlaveState.havenot_time;

                            //Console.WriteLine("Обработка пакета");
                            logger.Info("Обработка пакета");
                            processing_infopaket(receive_packet_data);
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

        /// <summary>
        /// обработка статусов
        /// </summary>
        private void processing_singleregx(ushort date)
        {
            if (slave!=null)
            {
                if (slave.DataStore.HoldingRegisters[1] != SlaveState.havechecktotime)
                {
                    logger.Info("Состояние slave" + slave.DataStore.HoldingRegisters[1]);
                    //если данные больше 100, то это кол-ве байт
                    if (date > 70)
                    {
                        countDataStruct = Convert.ToInt32(date);
                        //Console.WriteLine("Получен объем данных в байт:" + countDataStruct);
                        logger.Info("Получен объем данных в байт:" + countDataStruct);

                        int dataushort = (countDataStruct / 2) + 1;
                        //Console.WriteLine("Получен объем данных в ushort:" + dataushort);
                        logger.Info("Получен объем данных в ushort:" + dataushort);

                        data_byte_for_processing = new byte[countDataStruct];
                        countDataStructUsshort = (countDataStruct / 2) + 1;

                        //в случии если пакет прервался то обнуляем
                        countrecivedcount = 0;
                    }

                    slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                    logger.Info("Slave перешел в состоянии передача" + slave.DataStore.HoldingRegisters[1]);

                }
                if (slave.DataStore.HoldingRegisters[1] == SlaveState.havechecktotime)
                {
                    logger.Info("Состояние slave" + slave.DataStore.HoldingRegisters[1]);
                    logger.Info("Состояние slave проверка контрольной суммы");
                    // В случае если идет проверка системы
                    if (cr16 == date)
                    {
                        logger.Info("Сумма совпала");

                        slave.DataStore.HoldingRegisters[5] = StateCR.haveNotError;
                        logger.Info("В регистр проверки контрольной суммы записался (проверка CR16 состоялась)" + slave.DataStore.HoldingRegisters[5]);

                        slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                        logger.Info("В регистр состояние (проверка CR16 состоялась)" + slave.DataStore.HoldingRegisters[1]);

                        //logger.Info("Сумма совпала " + SlaveState.have_free_time);
                    }
                }
            }
            if (modbusTcp!=null)
            {
                if (modbusTcp.DataStore.HoldingRegisters[1] != SlaveState.havechecktotime)
                {
                    logger.Info("Состояние slave" + modbusTcp.DataStore.HoldingRegisters[1]);
                    //если данные больше 100, то это кол-ве байт
                    if (date > 70)
                    {
                        countDataStruct = Convert.ToInt32(date);
                        //Console.WriteLine("Получен объем данных в байт:" + countDataStruct);
                        logger.Info("Получен объем данных в байт:" + countDataStruct);

                        int dataushort = (countDataStruct / 2) + 1;
                        //Console.WriteLine("Получен объем данных в ushort:" + dataushort);
                        logger.Info("Получен объем данных в ushort:" + dataushort);

                        data_byte_for_processing = new byte[countDataStruct];
                        countDataStructUsshort = (countDataStruct / 2) + 1;
                    }

                    modbusTcp.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                    logger.Info("Slave перешел в состоянии передача" + modbusTcp.DataStore.HoldingRegisters[1]);

                }
                if (modbusTcp.DataStore.HoldingRegisters[1] == SlaveState.havechecktotime)
                {
                    logger.Info("Состояние slave" + modbusTcp.DataStore.HoldingRegisters[1]);
                    logger.Info("Состояние slave проверка контрольной суммы");
                    // В случае если идет проверка системы
                    if (cr16 == date)
                    {
                        logger.Info("Сумма совпала");

                        modbusTcp.DataStore.HoldingRegisters[5] = StateCR.haveNotError;
                        logger.Info("В регистр проверки контрольной суммы записался (проверка CR16 состоялась)" + modbusTcp.DataStore.HoldingRegisters[5]);

                        modbusTcp.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                        logger.Info("В регистр состояние (проверка CR16 состоялась)" + modbusTcp.DataStore.HoldingRegisters[1]);

                        //logger.Info("Сумма совпала " + SlaveState.have_free_time);
                    }
                }
            }


        }


        /// <summary>
        /// обработка статусов
        /// </summary>
        private void processing_infopaket(ushort[] date)
        {
            Console.WriteLine("Обработка инфопакета Slave занят:");
            Console.WriteLine("Обработка инфопакета:");
            //перводим в массив байт
            Buffer.BlockCopy(date, 0, receivedpacket, 0, receivedpacket.Length);

            countrecivedcount += receivedpacket.Length;
            Console.WriteLine("Получено:" + countDataStructUsshort);
            if (countrecivedcount > countDataStruct)
            {
                processing_infopaket_endl();
            }
            // начало
            if (countrecivedcount == receivedpacket.Length)
            {
                processing_infopaket_inception();
            }

            if (countrecivedcount > receivedpacket.Length && countrecivedcount < countDataStruct)
            {
                processing_infopaket_middle();
            }
            Console.WriteLine("Переданно " + countrecivedcount);
        }

        /// <summary>
        /// Обработка первого пакета
        /// </summary>
        private void processing_infopaket_inception()
        {
            logger.Info("Получен первый инфопакет:");
            for (int i = 0; i < receivedpacket.Length; i++)
            {
                data_byte_for_processing[i] = receivedpacket[i];
            }

            //изменение состояние

            if (slave!=null)
            {
                slave.DataStore.HoldingRegisters[1] = SlaveState.havetimetransfer;
                logger.Info("В регистр состояние" + slave.DataStore.HoldingRegisters[1]);
            }
            if (modbusTcp!=null)
            {
                modbusTcp.DataStore.HoldingRegisters[1] = SlaveState.havetimetransfer;
                logger.Info("В регистр состояние" + modbusTcp.DataStore.HoldingRegisters[1]);
            }


            logger.Info("Обработан первый инфопакет:");
            writebyte(receivedpacket);
        }
        
        /// <summary>
        /// Обработка середины
        /// </summary>
        private void processing_infopaket_middle()
        {
            try
            {
                logger.Info("Получен серединный инфопакет");
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
                    logger.Info("В регистр состояние" + slave.DataStore.HoldingRegisters[1]);
                    logger.Info("Получен серединный инфопакет");
                    writebyte(receivedpacket);
                }
                if (modbusTcp!=null)
                {
                    //изменение состояние
                    modbusTcp.DataStore.HoldingRegisters[1] = SlaveState.havetimetransfer;
                    logger.Info("В регистр состояние" + modbusTcp.DataStore.HoldingRegisters[1]);
                    logger.Info("Получен серединный инфопакет");
                    writebyte(receivedpacket);
                }    
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
        
        /// <summary>
        /// Обработка конца пакета
        /// </summary>
        private void processing_infopaket_endl()
        {
            try
            {
                //Console.WriteLine("Получен конечный инфопакет:");
                logger.Info("Получен конечный инфопакет:");
                
                int delta_start_mid = countrecivedcount - receivedpacket.Length;
                int delta_countreciveAndSend = Math.Abs(countDataStruct - delta_start_mid);
                for (int i = 0; i < delta_countreciveAndSend; i++)
                {
                    data_byte_for_processing[delta_start_mid + i] = receivedpacket[i];
                }

                logger.Info("Получен конечный инфопакет обработка:");
                writebyte(data_byte_for_processing);

                //Обнуление переданных пакетов
                logger.Info("обнуление переданных пакетов");
                countrecivedcount = 0;

                //собираем класс
                Console.WriteLine("Архив и десерелизация объекта");
                //Сlass_Deserialization(data_byte_for_processing);
                Archive_Code(data_byte_for_processing);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// Десерилизация класса
        /// </summary>
        private void Сlass_Deserialization(byte[] date)
        {
            try
            {
                logger.Info("Формирование класса:");

                Stream stream = new MemoryStream(date);
                BinaryFormatter formatter = new BinaryFormatter();

                metaClass = (MetaClassForStructandtherdata)formatter.Deserialize(stream);

                logger.Info("Сформирован мета-класс");
                //После обработки статус меняется на свободный
                SignalFormedMetaClass?.Invoke(metaClass.struct_which_need_transfer);   // 2.Вызов события
                logger.Info("Вызвано события на изменения");

                //Контрольная сумма
                Crc16 crc16 = new Crc16();
                byte[] crc16bytes = crc16.ComputeChecksumBytes(date);

                cr16 = crc16.convertoshort(crc16bytes);
                logger.Info("Сформирована контрольная сумма");

                if (slave!=null)
                {
                    slave.DataStore.HoldingRegisters[1] = SlaveState.havechecktotime;
                    logger.Info("В регистр состояние готов принимать пакеты" + slave.DataStore.HoldingRegisters[1]);
                }
                if (modbusTcp!=null)
                {
                    modbusTcp.DataStore.HoldingRegisters[1] = SlaveState.havechecktotime;
                    logger.Info("В регистр состояние готов принимать пакеты" + modbusTcp.DataStore.HoldingRegisters[1]);
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                have_error_for_deseration();
                Console.WriteLine(ex);
            }
        }


        private void Archive_Code(byte[] date)
        {
            try
            {
                MemoryStream stream = new MemoryStream(date);
                BinaryFormatter formatter = new BinaryFormatter();
                logger.Info("Декомпрессия");
                MemoryStream memory =decompress(stream,false);
                byte[] class_outdecompress = memory.ToArray();

                logger.Info("Формирование класса:");
                Сlass_Deserialization(class_outdecompress);
            }
            catch(Exception ex)
            {
                have_error_for_deseration();
                logger.Error(ex);
            }
        }

        private void have_error_for_deseration()
        {
            if (slave != null)
            {
                slave.DataStore.HoldingRegisters[1] = SlaveState.haveerror;
                logger.Error("Ошибка десеризация");

                ///time reboot
                Thread.Sleep(1000);
                update_after_error();
            }
            if (modbusTcp != null)
            {
                modbusTcp.DataStore.HoldingRegisters[1] = SlaveState.haveerror;
                logger.Error("Ошибка десеризация");
                Thread.Sleep(1000);
                update_after_error();
            }
        }

        private void update_after_error()
        {
            if (slave != null)
            {
                slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                logger.Error("Востановление состояние");
            }
            if (modbusTcp != null)
            {
                modbusTcp.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;
                logger.Error("Востановление состояние");
            }
        }

        /// <summary>
        /// Вывод в консоль
        /// </summary>
        /// <param name="date"></param>
        private void writebyte(byte[] date)
        {
            Console.WriteLine("Вывод пакета байт:");

            for (int i = 0; i < date.Length; i++)
            {
                Console.Write(date[i] + " ");
            }
            Console.WriteLine("");
        }



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
