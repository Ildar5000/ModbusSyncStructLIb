using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Specialized;
using System.IO.Ports;

namespace ModbusSyncStructLIb
{
    public class PropertiesSetting
    {
        public string PortName;
        public int BaudRate;
        public int DataBits;
        public string Parity;
        public string StopBits;

        public PropertiesSetting()
        {

            PortName=Properties.SettingMasterSlave.Default.Port;
            BaudRate= Properties.SettingMasterSlave.Default.BaudRate;
            DataBits = Properties.SettingMasterSlave.Default.DataBits;
            
            //Parity= Properties.SettingMasterSlave.Default.Parity;
            

        }


    }
}
