using ModbusSyncStructLIb.DespriptionState;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ModbusSyncStructLIb.CheckConnect
{
    public class ManagerConnectionModbus
    {
        MasterSyncStruct master;
        Logger logger;

        SlaveSyncSruct slave;

        bool islive = true;
        public bool have_connection = false;

        Random rand = new Random();

        ushort crtime = 0;

        public ManagerConnectionModbus(MasterSyncStruct master)
        {
            logger = LogManager.GetCurrentClassLogger();

            this.master = master;
            rand = new Random();
        }

        public ManagerConnectionModbus(SlaveSyncSruct slave)
        {
            logger = LogManager.GetCurrentClassLogger();

            this.slave = slave;

            rand = new Random();
        }


        public void stop()
        {
            if (master.master!=null)
            {
                master.close();
                
            }
            

            if (slave!= null)
            {
                slave.close();

            }
            if (islive==true)
            {
                restart();
            }
            
        }

        public void restart()
        {
            Thread.Sleep(1000);
            start();
        }


        public void start()
        {
            if (master!=null)
            {
                masterstart();
            }

            if (slave != null)
            {
                slavestart();
            }

        }

        public void timeclick(object date)
        {
            ushort dateold = (ushort)date;
            Thread.Sleep(200);
            if (dateold== crtime)
            {
                //stop();
                logger.Warn("Канал завис");          
            }
            
        }

        public void masterstart()
        {
            try
            {
                master.Open();
                
                master.master.Transport.SlaveBusyUsesRetryCount = true;
                master.master.Transport.WaitToRetryMilliseconds = 100;
                master.master.Transport.Retries = 5;
                //master.master.Transport.WriteTimeout = 50;
                //master.master.Transport.ReadTimeout = 50;
                crtime = (ushort)rand.Next(11,100);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                stop();
            }
            while (islive)
            {
                try
                {
                    Thread wating = new Thread(new ParameterizedThreadStart(timeclick));
                    wating.Start(crtime);

                    master.master.WriteSingleRegister(master.slaveID, TableUsedforRegisters.diagnostik_send, crtime);
                    logger.Trace("Cвязь присутствует");

                    have_connection = true;
                    Thread.Sleep(100);

                    ushort[] getrex=master.master.ReadHoldingRegisters(master.slaveID, TableUsedforRegisters.diagnostik_send, 1); 
                    wating.Abort();

                    have_connection = true;
                    if (crtime == getrex[0])
                    {
                        logger.Trace("Обратная связь присутствует");
                        crtime = (ushort)rand.Next(11, 100);
                    }
                    else
                    {
                        logger.Trace("Обратная связь отсутствует");
                    }
                    crtime = (ushort)rand.Next(11, 100);

                    Thread.Sleep(2000);
                }
                catch (Exception ex)
                {
                    logger.Trace("Cвязь Отсутствует");
                    have_connection = false;
                    stop();
                    //Console.WriteLine(ex);
                    have_connection = false;
                    //logger.Error(ex);
                    Thread.Sleep(2000);
                    break;
                }
            }

            if (islive==false)
            {
                return;
            }
        }

        public void slavestart()
        {

            try
            {
                slave.Open();
            }
            catch(Exception ex)
            {

                logger.Error(ex);
            }
            while (islive)
            {
               Console.WriteLine("work");
               Thread.Sleep(200);
               if (crtime!= slave.randnumber)
               {
                    logger.Trace("Есть связь");
                    crtime = slave.randnumber;
                    have_connection = true;
                    Thread.Sleep(5000);
                    
                }
               else
               {
                    crtime = slave.randnumber;
                    logger.Warn("Нету связи");
                    have_connection = false;
                    Thread.Sleep(5000);
                }
            }
        }

        public void closeManager()
        {
            islive = false;
        }

    }
}
