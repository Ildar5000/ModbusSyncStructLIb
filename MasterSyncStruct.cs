﻿using System;
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

        public byte slaveID;
        
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
                Console.WriteLine(propertiesSetting.PortName);
                serialPort.Open();
                SerialPortAdapter = new SerialPortAdapter(serialPort);

                if (TypeModbus==0)
                {
                    master = ModbusSerialMaster.CreateRtu(SerialPortAdapter);
                }

                if (TypeModbus == 1)
                {
                    master = ModbusSerialMaster.CreateAscii(SerialPortAdapter);
                }

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
        /// чтение статусе у Slave
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


        /// <summary>
        /// отправка пакета с изменением статуса
        /// </summary>
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

        /// <summary>
        /// отправка инфоданных
        /// </summary>
        public void send_multi_message(MemoryStream stream)
        {
            //Мастер занят
            state_master = 1;

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


                        master.WriteMultipleRegisters(slaveID, coilAddress, date_modbus);
                    }

                }
                else  //В случае если не получено данные
                {
                    //Console.WriteLine("Пакет не может передаться, связи с тем, что Slave занят");
                    logger.Warn("Пакет не может передаться, связи с тем, что Slave занят");
                    int count_try=0;

                    repeat(stream, count_try);

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
                int counter_reguest_status = 0;

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
                    counter_reguest_status = 0;
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
            master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
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

            Console.WriteLine("Cформирован");
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
            k = 0;
            //Console.WriteLine("Отправка данных");
            logger.Trace("Отправка данных");

            //если slave свободен то отправляем
            master.WriteMultipleRegisters(slaveID, coilAddress, sentpacket);
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
        public void repeat(MemoryStream stream,int count_try_recurs)
        {
            if (count_try_recurs != 3)
            {
                //Мастер занят
                state_master = 1;

                ushort coilAddress = 10;
                byte[] date = stream.ToArray();

                int count = 50;
                count = (date.Length / 2) + 1;
                ushort[] date_modbus = new ushort[date.Length / 2 + 1];

                int needtopacketsend;

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
                        logger.Info("Отправляем метапкет с кол-вом данных байт: " + date.Length);
                        logger.Info("Отправляем метапкет с кол - вом данных ushort: " + date_modbus.Length);

                        //если данные больше чем переданных
                        if (date_modbus.Length > count_send_packet)
                        {
                            morethantransfer(coilAddress, date_modbus, count_send_packet, sentpacket, date);
                            
                            Console.WriteLine("Cформирован");
                            state_master = 0;
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
                            return;
                        }

                    }
                    else  //В случае если не получено данные
                    {
                        //Console.WriteLine("Пакет не может передаться, связи с тем, что Slave занят");
                        logger.Warn("Пакет не может передаться, связи с тем, что Slave занят");

                        Thread.Sleep(1000);
                        
                        count_try_recurs++;
                        repeat(stream, count_try_recurs);

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

            logger.Info("Ожидание ответа");

            //Мастер свободен
            state_master = 0;

            Thread.Sleep(50);

            //logger.Info("Отправка данных");

            //status_slave = SendRequestforAnyStatusSlave(5);
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
