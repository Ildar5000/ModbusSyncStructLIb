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

namespace ModbusSyncStructLIb
{
    public class SlaveSyncSruct
    {
        SerialPort serialPort;
        ModbusSlave slave;

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

            receivedpacket = new byte[count_send_packet*2];
            receive_packet_data = new ushort[count_send_packet];
            //data_byte= new byte[count_send_packet*2];
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
                slave.DataStore.HoldingRegisters[2] = 333;

                slave.DataStore.HoldingRegisters[3] = 433;

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
        }

        private void writebyte(byte[] date)
        {
            Console.WriteLine("Вывод пакета байт:");
            for (int i=0;i<date.Length;i++)
            {
                Console.Write(date[i]+" ");
            }
            Console.WriteLine("");
        }

        //обработка статусов
        private void processing_infopaket(ushort[] date)
        {
            Console.WriteLine("Обработка инфопакета:");
            //перводим в массив байт
            Buffer.BlockCopy(date, 0, receivedpacket, 0, receivedpacket.Length);


            //конечное число

            countrecivedcount += date.Length;
            Console.WriteLine("Получено:"+countDataStructUsshort);
            if (countrecivedcount> countDataStructUsshort)
            {
                Console.WriteLine("Получен конечный инфопакет:");
                int delta_countreciveAndSend = Math.Abs(countrecivedcount - countDataStruct);
                
                for (int i = 0; i < delta_countreciveAndSend; i++)
                {
                    data_byte_for_processing[countrecivedcount + i] = receivedpacket[i];
                }

                Console.WriteLine("Получен конечный инфопакет:");
                writebyte(data_byte_for_processing);
                
                countrecivedcount = 0;
                Сlass_Deserialization(data_byte_for_processing);
                //slave.DataStore.HoldingRegisters[0] = SlaveState.have_free_time;
            }

            // начало
            if (countrecivedcount== count_send_packet)
            {
                Console.WriteLine("Получен первый инфопакет:");
                for (int i = 0; i < receivedpacket.Length;i++)
                {
                    data_byte_for_processing[i] = receivedpacket[i];
                }
                Console.WriteLine("Получен первый инфопакет:");
                writebyte(data_byte_for_processing);


                //slave.DataStore.HoldingRegisters[0] = SlaveState.have_free_time;
            }
            if (countrecivedcount > countDataStructUsshort && countrecivedcount<= countDataStructUsshort)
            {
                Console.WriteLine("Получен серединный инфопакет:");
                int delta_countreciveAndSend = countDataStruct- countrecivedcount;

                for (int i = 0; i < receivedpacket.Length; i++)
                {
                    data_byte_for_processing[countrecivedcount + i] = receivedpacket[i];
                }
                Console.WriteLine("Получен серединный инфопакет:");
                writebyte(data_byte_for_processing);
            }
            
            Console.WriteLine("Переданно "+countrecivedcount);
        }

        private void Сlass_Deserialization(byte[] date)
        {
            Console.WriteLine("Формирование класса:");
            Stream stream = new MemoryStream(date);
            BinaryFormatter formatter = new BinaryFormatter();

            MetaClassForStructandtherdata metaClass = (MetaClassForStructandtherdata)formatter.Deserialize(stream);

            Console.WriteLine("Cформарован");

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
                        Console.WriteLine("Пришла команда на обработку"+e.Data.B[0]);
                    }

                    //slave.DataStore.HoldingRegisters[1] = SlaveState.havenot_time;
                    if (e.Data.B.Count > 1)
                    {
                        Console.WriteLine("Пришел пакет с данными:");
                        for (int i = 0; i < e.Data.B.Count; i++)
                        {
                            receive_packet_data[i] = e.Data.B[i];
                            Console.Write(receive_packet_data[i]+" ");
                        }
                        Console.WriteLine("");
                        Console.WriteLine("Обработка пакета");
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
