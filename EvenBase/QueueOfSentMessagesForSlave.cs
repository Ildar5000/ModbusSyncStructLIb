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
            Thread thread = new Thread(Recursve);
            thread.Start();
        }

        public QueueOfSentMessagesForSlave(MasterSyncStruct m_master)
        {
            this.master = m_master;
            Thread thread = new Thread(Recursve);
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
        public void AddQueue(MemoryStream message)
        {
            numbers.Enqueue(message);
            count++;

        } 

        public void Send()
        {
            if (count!=0)
            {
                MemoryStream memory = numbers.Dequeue();
                master.SendMultiMessage(memory);
                count--;
            }
        }

        public void StopTransfer()
        {
            try
            {
                if (master!=null)
                {
                    master.stoptransfer();
                    clear_queue();
                }
            }
            catch(Exception ex)
            {

            }
            
            
        }


        public void Recursve()
        {
            while(true)
            {
                if (count!=0 )
                {
                    if (master.stoptransfer_signal==true)
                    {
                        numbers.Clear();
                        count = 0;
                        master.stoptransfer_signal = false;
                    }
                    else
                    {
                        if (master.state_master == 0 && numbers.Count != 0)
                        {
                            MemoryStream memory = numbers.Dequeue();
                            master.SendMultiMessage(memory);
                            count--;
                        }
                        //Случий с ошибкой на мастере
                        if (master.state_master == 1)
                        {
                            clear_queue();
                            master.state_master = 0;
                        }
                    }
                }
            }
        }
    }
}
