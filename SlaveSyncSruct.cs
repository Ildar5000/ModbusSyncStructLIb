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

namespace ModbusSyncStructLIb
{
    public class SlaveSyncSruct
    {
        public SerialPort serialPort;
        ModbusSlave slave;

        private static Logger logger;
        //События
        #region События
        public delegate void SignalFormedMetaClassMethod(object struct_which_need_transfer);
        public event SignalFormedMetaClassMethod SignalFormedMetaClass;

        #endregion

        /// <summary>
        /// Кол-во пакетов в одном запросе
        /// </summary>
        /// 
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
        byte slaveID;
        byte[] receivedpacket;
        public MetaClassForStructandtherdata metaClass;

        /// <summary>
        /// Контрольная сумма
        /// </summary>
        private ushort cr16;

        public SlaveSyncSruct()
        {
            var loggerconf = new XmlLoggingConfiguration("NLog.config");
            logger = LogManager.GetCurrentClassLogger();

            PropertiesSetting propertiesSetting = new PropertiesSetting();
            slaveID = 1;
            serialPort = new SerialPort(propertiesSetting.PortName);
            serialPort.PortName = propertiesSetting.PortName;
            serialPort.BaudRate = propertiesSetting.BaudRate;
            serialPort.DataBits = propertiesSetting.DataBits;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            receivedpacket = new byte[count_send_packet*2];
            receive_packet_data = new ushort[count_send_packet];
            //data_byte= new byte[count_send_packet*2];

            metaClass = new MetaClassForStructandtherdata();
        }

        public void Open()
        {
            try
            {
                serialPort.Open();
                slave = ModbusSerialSlave.CreateRtu(slaveID, serialPort);
                
                slave.DataStore = Modbus.Data.DataStoreFactory.CreateDefaultDataStore();
                slave.DataStore.DataStoreWrittenTo += new EventHandler<DataStoreEventArgs>(Modbus_DataStoreWriteTo);

                slave.DataStore.HoldingRegisters[1] = 0;

                Console.WriteLine(slave.DataStore.HoldingRegisters[1]);

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
                LogLevel level = LogLevel.Error;
                log.Log(level, ex.Message);
            }

        }

        public void close()
        {
            serialPort.Close();
        }

        //обработка статусов
        private void processing_singleregx(ushort date)
        {
            //если данные больше 100, то это кол-ве байт
            if (date>70)
            {
                countDataStruct = Convert.ToInt32(date);
                Console.WriteLine("Получен объем данных в байт:"+ countDataStruct);
                int dataushort = (countDataStruct / 2) + 1;
                Console.WriteLine("Получен объем данных в ushort:" + dataushort);
                data_byte_for_processing = new byte[countDataStruct];
                countDataStructUsshort = (countDataStruct / 2) + 1;
            }

            
            // В случае если идет проверка системы
            if (slave.DataStore.HoldingRegisters[1] == SlaveState.havechecktotime)
            {
                if (cr16== date)
                {
                    logger.Info("Сумма совпала");
                    //Регистр с проверкой контрольной суммы
                    slave.DataStore.HoldingRegisters[5] = StateCR.haveNotError;
                    //Регистр со статусом
                    slave.DataStore.HoldingRegisters[1] = SlaveState.have_free_time;

                }

                //slave.WriteComplete();
            }
        }

        /// <summary>
        /// Вывод в консоль
        /// </summary>
        /// <param name="date"></param>
        private void writebyte(byte[] date)
        {
            Console.WriteLine("Вывод пакета байт:");
            
            for (int i=0;i<date.Length;i++)
            {
                Console.Write(date[i]+" ");
            }
            Console.WriteLine("");
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
            Console.WriteLine("Получено:"+countDataStructUsshort);
            if (countrecivedcount> countDataStruct)
            {
                try
                {
                    Console.WriteLine("Получен конечный инфопакет:");

                    int delta_start_mid = countrecivedcount - receivedpacket.Length;
                    int delta_countreciveAndSend = Math.Abs(countDataStruct - delta_start_mid);
                    for (int i = 0; i < delta_countreciveAndSend; i++)
                    {
                        data_byte_for_processing[delta_start_mid+i] = receivedpacket[i];
                    }

                    Console.WriteLine("Получен конечный инфопакет:");
                    writebyte(data_byte_for_processing);

                    countrecivedcount = 0;
                    Сlass_Deserialization(data_byte_for_processing);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
            }

            // начало
            if (countrecivedcount== receivedpacket.Length)
            {
                Console.WriteLine("Получен первый инфопакет:");
                for (int i = 0; i < receivedpacket.Length;i++)
                {
                    data_byte_for_processing[i] = receivedpacket[i];
                }
                Console.WriteLine("Получен первый инфопакет:");
                writebyte(receivedpacket);

            }
            if (countrecivedcount > receivedpacket.Length && countrecivedcount< countDataStruct)
            {
                try
                {
                    Console.WriteLine("Получен серединный инфопакет:");
                    int delta_countreciveAndSend = countDataStruct - countrecivedcount;
                    int delta_start_mid = countrecivedcount - receivedpacket.Length;
                    for (int i = 0; i < receivedpacket.Length; i++)
                    {
                        data_byte_for_processing[delta_start_mid + i] = receivedpacket[i];
                    }
                    Console.WriteLine("Получен серединный инфопакет:");
                    writebyte(receivedpacket);
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
                
            }
            Console.WriteLine("Переданно "+countrecivedcount);
        }
        
        /// <summary>
        /// Серелизация класса
        /// </summary>
        private void Сlass_Deserialization(byte[] date)
        {
            try
            {
                Console.WriteLine("Формирование класса:");
                Stream stream = new MemoryStream(date);
                BinaryFormatter formatter = new BinaryFormatter();

                metaClass = (MetaClassForStructandtherdata)formatter.Deserialize(stream);

                //После обработки статус меняется на свободный
                SignalFormedMetaClass?.Invoke(metaClass.struct_which_need_transfer);   // 2.Вызов события


                //Контрольная сумма
                Crc16 crc16 = new Crc16();
                byte[] crc16bytes = crc16.ComputeChecksumBytes(date);

                cr16 = crc16.convertoshort(crc16bytes);

                slave.DataStore.HoldingRegisters[1] = SlaveState.havechecktotime;

                Console.WriteLine("Cформирован");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
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
                        processing_singleregx(e.Data.B[0]);
                        //Console.WriteLine("Пришла команда на обработку"+e.Data.B[0]);
                        logger.Info("Пришла команда на обработку" + e.Data.B[0]);
                    }

                    if (e.Data.B.Count > 1)
                    {
                        //Console.WriteLine("Пришел пакет с данными:");
                        logger.Info("Пришел пакет с данными:");
                        for (int i = 0; i < e.Data.B.Count; i++)
                        {
                            receive_packet_data[i] = e.Data.B[i];
                            Console.Write(receive_packet_data[i]+" ");
                        }
                        Console.WriteLine("");
                        //Console.WriteLine("Обработка пакета");
                        logger.Info("Обработка пакета");
                        processing_infopaket(receive_packet_data);
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
