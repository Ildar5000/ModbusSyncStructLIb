using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb.DespriptionState
{
    public class SlaveState
    {
        public const int have_free_time = 0;
        public const int havenot_time = 1;
        public const int haveerror = 2;
        public const int havechecktotime = 3;
        public const int havetimereboot = 4;
        public const int havetimetransfer = 5;
        public const int havechecktotimeOK = 6;


        int currentstatusslave;
        public SlaveState()
        {

        }

        /// <summary>
        /// Узнать статус Slave
        /// </summary>
        /// <param name="status"></param>
        public void KnowStatusSlaveNow(ushort status)
        {

        }

    }
}
