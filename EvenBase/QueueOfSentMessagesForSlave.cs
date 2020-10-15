﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb.EvenBase
{
    public class QueueOfSentMessagesForSlave
    {
        public MasterSyncStruct master;
        Queue<MemoryStream> numbers = new Queue<MemoryStream>();
        int count = 0;

        public QueueOfSentMessagesForSlave()
        {

        }

        public QueueOfSentMessagesForSlave(MasterSyncStruct m_master)
        {
            this.master = m_master;
        }

        //добавлен в очереди
        public void add_queue(MemoryStream message)
        {
            numbers.Enqueue(message);
            count++;
            send();
        }

        public void send()
        {
            if (count!=0)
            {
                MemoryStream memory = numbers.Dequeue();
                master.send_multi_message(memory);
                count--;
            }
        }
    }
}
