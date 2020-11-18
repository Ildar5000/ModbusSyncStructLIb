using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb.ControlCheck
{
    class Crc16
    {
        const ushort polynomial = 0xA001;
        ushort[] table = new ushort[256];
        
        /// <summary>
        /// Расчет суммы
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public ushort ComputeChecksum(byte[] bytes)
        {
            ushort crc = 0;
            for (int i = 0; i < bytes.Length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            return crc;
        }

        // <summary>
        /// Сумма контролььная финальная
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public byte[] ComputeChecksumBytes(byte[] bytes)
        {
            ushort crc = ComputeChecksum(bytes);
            return BitConverter.GetBytes(crc);
        }

        public Crc16()
        {
            ushort value;
            ushort temp;
            for (ushort i = 0; i < table.Length; ++i)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }
        }
        
        
        /// <summary>
        /// Конвертирует в ushort
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        
        public ushort ConverToShort(byte[] date)
        {
            ushort[] date_modbus=new ushort[1];
            Buffer.BlockCopy(date, 0, date_modbus, 0, date.Length);

            return date_modbus[0];

        }

    }
}
