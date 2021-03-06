﻿using ModbusSyncStructLIb.DespriptionState;
using NLog;
using NLog.Config;
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
        private static Logger logger;
        public MasterSyncStruct master;
        Queue<MemoryStream> numbers = new Queue<MemoryStream>();
        int count = 0;

        public bool startsend=false;

        public QueueOfSentMessagesForSlave()
        {
            Thread thread = new Thread(Recursve);
            thread.Start();
            var loggerconf = new XmlLoggingConfiguration("NLog.config");
            logger = LogManager.GetCurrentClassLogger();
        }

        public QueueOfSentMessagesForSlave(MasterSyncStruct m_master)
        {
            var loggerconf = new XmlLoggingConfiguration("NLog.config");
            logger = LogManager.GetCurrentClassLogger();
            this.master = m_master;
            
            Thread thread = new Thread(Recursve);
            thread.Start();
        }

        /// <summary>
        /// Очистка очереди
        /// </summary>
        public void ClearQueue()
        {
            numbers.Clear();
        }

        /// <summary>
        /// добавлен в очереди
        /// </summary>
        public void AddQueue(MemoryStream message)
        {
            startsend = true;
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
                    count = 0;
                    master.StopTransfer();
                    ClearQueue();
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
                if (numbers.Count != 0 )
                {
                    if (master.stoptransfer_signal==true)
                    {
                        ClearQueue();
                        count = 0;
                        Thread.Sleep(100);
                        master.stoptransfer_signal = false;
                        startsend = false;
                    }
                    else
                    {
                        if (master.state_master != SlaveState.haveerror && numbers.Count != 0)
                        {
                            MemoryStream memory = numbers.Dequeue();
                            startsend = true;
                            master.SendMultiMessage(memory);
                            count--;
                            startsend = false;
                        }
                        //Случий с ошибкой на мастере
                        if (master.state_master == SlaveState.haveerror)
                        {
                            ClearQueue();
                            count = 0;
                            master.state_master = 0;
                            startsend = false;
                            logger.Error("Очередь: Не удалось отправить");
                        }
                    }
                }
            }
        }
    }
}
