using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb
{
    public interface EventSubscriptionInterface
    {
        void Start_Subscription();

        void execution_processing_reguest();


    }
}
