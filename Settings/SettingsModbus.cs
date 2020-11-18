using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb.Settings
{
    [Serializable]
    public class SettingsModbus
    {
        public string ComName { get; set; }
        public int BoudRate { get; set; }
        public int DataBits { get; set; }
        public string Party_type_str { get; set; }
        public string StopBits_type_str { get; set; }

        public int Party_type_int { get; set; }
        public int StopBits_type_int { get; set; }

        public int ReadTimeout;
        public int WriteTimeout;

        public string IP_client { get; set; }

        public int port_IP_client { get; set; }

        public int typeModbus { get; set; }

        public string typeModbusSTR { get; set; }

        public byte slaveID { get; set; }

        public int defaulttypemodbus { get; set; }

        public double deltatime { get; set; }

        public bool try_reboot_connection { get; set; }


        public SettingsModbus()
        {
            this.ComName = "COM1";
            this.BoudRate = 9600;
            this.DataBits = 8;
            this.Party_type_str = "Parity.None";
            this.StopBits_type_str = "StopBits.One";
            this.Party_type_int = (int)Parity.None;
            this.StopBits_type_int = (int)StopBits.One;
            this.ReadTimeout = 1000;
            this.WriteTimeout = 1000;
            IP_client = "127.0.0.1";
            port_IP_client = 502;
            typeModbus = 0;
            slaveID = 1;
        }

        public SettingsModbus(string ComName, int BoudRate, int DataBits, string Party_type, string StopBits_type, int ReadTimeout, int WriteTimeout,string IP_client,int port_IP_client,int typeModbus,byte slaveID, string typeModbusSTR)
        {
            this.ComName = ComName;
            this.BoudRate = BoudRate;
            this.DataBits = DataBits;
            this.Party_type_str = Party_type;
            this.StopBits_type_str = StopBits_type;
            this.ReadTimeout = ReadTimeout;
            this.WriteTimeout = WriteTimeout;
            this.IP_client = IP_client;
            this.port_IP_client = port_IP_client;
            this.typeModbus = typeModbus;
            this.typeModbusSTR = typeModbusSTR;
            this.slaveID = slaveID;

            /*
            typeParitylist.Add("None");
            typeParitylist.Add("Even");
            typeParitylist.Add("Mark");
            typeParitylist.Add("Odd");
            typeParitylist.Add("Space");

            typeStopBitslist.Add("None");
            typeStopBitslist.Add("One");
            typeStopBitslist.Add("OnePointFive");
            typeStopBitslist.Add("Two");
            */

            switch (Party_type_str)
            {
                case "Space":
                    Party_type_int = (int)Parity.Space;
                    break;
                case "Even":
                    Party_type_int = (int)Parity.Even;
                    break;
                case "Mark":
                    Party_type_int = (int)Parity.Mark;
                    break;
                case "Odd":
                    Party_type_int = (int)Parity.Odd;
                    break;
                default:
                    //none
                    Party_type_int = (int)Parity.None;
                    break;
            }

            switch (StopBits_type_str)
            {
                case "One":
                    Party_type_int = (int)StopBits.One;
                    break;
                case "OnePointFive":
                    Party_type_int = (int)StopBits.OnePointFive;
                    break;
                case "Two":
                    Party_type_int = (int)StopBits.Two;
                    break;
                default:
                    //none
                    Party_type_int = (int)StopBits.None;
                    break;
            }

        }

        public SettingsModbus(string ComName, int BoudRate, int DataBits, string Party_type, string StopBits_type, int ReadTimeout, int WriteTimeout, string IP_client, int port_IP_client, int typeModbus, byte slaveID, string typeModbusSTR,int defaulttypemodbus)
        {
            this.ComName = ComName;
            this.BoudRate = BoudRate;
            this.DataBits = DataBits;
            this.Party_type_str = Party_type;
            this.StopBits_type_str = StopBits_type;
            this.ReadTimeout = ReadTimeout;
            this.WriteTimeout = WriteTimeout;
            this.IP_client = IP_client;
            this.port_IP_client = port_IP_client;
            this.typeModbus = typeModbus;
            this.typeModbusSTR = typeModbusSTR;
            this.slaveID = slaveID;
            this.defaulttypemodbus = defaulttypemodbus;
            /*
            typeParitylist.Add("None");
            typeParitylist.Add("Even");
            typeParitylist.Add("Mark");
            typeParitylist.Add("Odd");
            typeParitylist.Add("Space");

            typeStopBitslist.Add("None");
            typeStopBitslist.Add("One");
            typeStopBitslist.Add("OnePointFive");
            typeStopBitslist.Add("Two");
            */

            switch (Party_type_str)
            {
                case "Space":
                    Party_type_int = (int)Parity.Space;
                    break;
                case "Even":
                    Party_type_int = (int)Parity.Even;
                    break;
                case "Mark":
                    Party_type_int = (int)Parity.Mark;
                    break;
                case "Odd":
                    Party_type_int = (int)Parity.Odd;
                    break;
                default:
                    //none
                    Party_type_int = (int)Parity.None;
                    break;
            }

            switch (StopBits_type_str)
            {
                case "One":
                    Party_type_int = (int)StopBits.One;
                    break;
                case "OnePointFive":
                    Party_type_int = (int)StopBits.OnePointFive;
                    break;
                case "Two":
                    Party_type_int = (int)StopBits.Two;
                    break;
                default:
                    //none
                    Party_type_int = (int)StopBits.None;
                    break;
            }

        }

        public SettingsModbus(string ComName, int BoudRate, int DataBits, string Party_type, string StopBits_type, int ReadTimeout, int WriteTimeout, string IP_client, int port_IP_client, int typeModbus, byte slaveID, string typeModbusSTR, int defaulttypemodbus,double deltatime)
        {
            this.ComName = ComName;
            this.BoudRate = BoudRate;
            this.DataBits = DataBits;
            this.Party_type_str = Party_type;
            this.StopBits_type_str = StopBits_type;
            this.ReadTimeout = ReadTimeout;
            this.WriteTimeout = WriteTimeout;
            this.IP_client = IP_client;
            this.port_IP_client = port_IP_client;
            this.typeModbus = typeModbus;
            this.typeModbusSTR = typeModbusSTR;
            this.slaveID = slaveID;
            this.defaulttypemodbus = defaulttypemodbus;
            this.deltatime = deltatime;


            switch (Party_type_str)
            {
                case "Space":
                    Party_type_int = (int)Parity.Space;
                    break;
                case "Even":
                    Party_type_int = (int)Parity.Even;
                    break;
                case "Mark":
                    Party_type_int = (int)Parity.Mark;
                    break;
                case "Odd":
                    Party_type_int = (int)Parity.Odd;
                    break;
                default:
                    //none
                    Party_type_int = (int)Parity.None;
                    break;
            }

            switch (StopBits_type_str)
            {
                case "One":
                    Party_type_int = (int)StopBits.One;
                    break;
                case "OnePointFive":
                    Party_type_int = (int)StopBits.OnePointFive;
                    break;
                case "Two":
                    Party_type_int = (int)StopBits.Two;
                    break;
                default:
                    //none
                    Party_type_int = (int)StopBits.None;
                    break;
            }

        }

        public SettingsModbus(string ComName, int BoudRate, int DataBits, string Party_type, string StopBits_type, int ReadTimeout, int WriteTimeout, string IP_client, int port_IP_client, int typeModbus, byte slaveID, string typeModbusSTR, int defaulttypemodbus, double deltatime,bool try_reboot_connection)
        {
            this.ComName = ComName;
            this.BoudRate = BoudRate;
            this.DataBits = DataBits;
            this.Party_type_str = Party_type;
            this.StopBits_type_str = StopBits_type;
            this.ReadTimeout = ReadTimeout;
            this.WriteTimeout = WriteTimeout;
            this.IP_client = IP_client;
            this.port_IP_client = port_IP_client;
            this.typeModbus = typeModbus;
            this.typeModbusSTR = typeModbusSTR;
            this.slaveID = slaveID;
            this.defaulttypemodbus = defaulttypemodbus;
            this.deltatime = deltatime;

            this.try_reboot_connection = try_reboot_connection;


            switch (Party_type_str)
            {
                case "Space":
                    Party_type_int = (int)Parity.Space;
                    break;
                case "Even":
                    Party_type_int = (int)Parity.Even;
                    break;
                case "Mark":
                    Party_type_int = (int)Parity.Mark;
                    break;
                case "Odd":
                    Party_type_int = (int)Parity.Odd;
                    break;
                default:
                    //none
                    Party_type_int = (int)Parity.None;
                    break;
            }

            switch (StopBits_type_str)
            {
                case "One":
                    Party_type_int = (int)StopBits.One;
                    break;
                case "OnePointFive":
                    Party_type_int = (int)StopBits.OnePointFive;
                    break;
                case "Two":
                    Party_type_int = (int)StopBits.Two;
                    break;
                default:
                    //none
                    Party_type_int = (int)StopBits.None;
                    break;
            }

        }




    }
}
