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
        public const byte StateSlaveRegisters = 0;

        /// <summary>
        /// ИД Слайва и Мастера
        /// </summary>
        public const byte SlaveId = 1;


        /// <summary>
        /// кол-во байт, которое нужно отправить
        /// </summary>
        public const byte SendDate = 2;

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

    }
}
