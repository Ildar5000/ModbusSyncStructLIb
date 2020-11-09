using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            Thread thread = new Thread(recursve);
            thread.Start();
        }

        public QueueOfSentMessagesForSlave(MasterSyncStruct m_master)
        {
            this.master = m_master;
            Thread thread = new Thread(recursve);
            thread.Start();
        }

        /// <summary>
        /// Очистка очереди
        /// </summary>
        public void clear_queue()
        {
            numbers.Clear();
        }

        /// <summary>
        /// добавлен в очереди
        /// </summary>
        public void add_queue(MemoryStream message)
        {
            numbers.Enqueue(message);
            count++;

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

        public void stoptransfer()
        {
            master.stoptransfer();
            clear_queue();
            
        }


        public void recursve()
        {
            while(true)
            {
                if (count!=0 )
                {
                    if (master.state_master == 0 && numbers.Count!=0)
                    {
                        MemoryStream memory = numbers.Dequeue();
                        master.send_multi_message(memory);
                        count--;
                    }
                    //Случий с ошибкой на мастере
                    if (master.state_master==1)
                    {
                        clear_queue();
                        master.state_master = 0;
                    }
                }
            }
        }
    }
}
