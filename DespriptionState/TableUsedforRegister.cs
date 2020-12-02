using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb.DespriptionState
{
    public class TableUsedforRegisters
    {
        /// <summary>
        /// Статус
        /// </summary>
        public const byte StateSlaveRegisters = 1;
        
        /// <summary>
        /// 
        /// </summary>
        public const byte StateSlaveRegistersformaster = 0;

        /// <summary>
        /// ИД Слайва и Мастера
        /// </summary>
        public const byte SlaveId = 1;


        /// <summary>
        /// кол-во байт, которое нужно отправить
        /// </summary>
        public const byte SendDate = 2;

        public const byte SendSecondDate = 3;
        /// <summary>
        /// Контрольная сумма
        /// </summary>
        public const ushort CR16 = 4;

        /// <summary>
        /// Контрольная сумма сделано
        /// </summary>
        public const ushort CR16_OK = 5;

        /// <summary>
        /// Контрольная архив
        /// </summary>
        public const ushort arcv = 6;
        
        /// <summary>
        /// Диагностика
        /// </summary>
        public const ushort diagnostik_send = 9;

        public const ushort start_send_regx = 10;
        /// <summary>
        /// Кол-во переднных какналов за 1 запрос
        /// </summary>
        public const int count_packet = 100;


        //public const int limitregxfortransfer = 60010;
        public const int limitregxfortransfer = 1010;


    }
}
