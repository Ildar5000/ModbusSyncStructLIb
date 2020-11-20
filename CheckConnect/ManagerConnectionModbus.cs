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
        public MasterSyncStruct master;
        public SlaveSyncSruct slave;

        public int deltatime=2000;

        public int deltatimeSlave = 2100;

        Logger logger;

        bool islive = true;
        public bool have_connection = false;

        Random rand = new Random();

        ushort crtime = 0;

        public ManagerConnectionModbus(MasterSyncStruct master)
        {
            logger = LogManager.GetCurrentClassLogger();

            deltatime = master.deltaTimeCheck;
            deltatimeSlave = deltatime + 100;
            this.master = master;
            rand = new Random();
        }

        public ManagerConnectionModbus(SlaveSyncSruct slave)
        {
            logger = LogManager.GetCurrentClassLogger();
            deltatime = slave.deltaTimeCheck;
            deltatimeSlave = deltatime + 100;
            this.slave = slave;

            rand = new Random();
        }


        public void Stop()
        {
            have_connection = false;

            if (master.master!=null)
            {
                master.Close();
                
            }
            if (slave!= null)
            {
                slave.Close();

            }
            if (islive==true)
            {
                Restart();
            }
        }

        public void Restart()
        {
            Thread.Sleep(1000);
            Start();
        }


        public void Start()
        {
            if (master!=null)
            {
                MasterStart();
            }

            if (slave != null)
            {
                SlaveStart();
            }

        }

        public void TimeWaitResponses(object date)
        {
            ushort dateold = (ushort)date;
            Thread.Sleep(200);
            if (dateold== crtime)
            {
                //stop();
                logger.Warn("Не отвечает Slave в течение нескольких минут");          
            }
            
        }

        public void MasterStart()
        {
            try
            {
                master.Open();
                master.master.Transport.SlaveBusyUsesRetryCount = true;
                master.master.Transport.WaitToRetryMilliseconds = 100;
                master.master.Transport.Retries = 5;
                crtime = (ushort)rand.Next(11,100);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                Stop();
            }
            while (islive)
            {
                try
                {
                    if (master.try_reboot_connection==true)
                    {
                        Thread wating = new Thread(new ParameterizedThreadStart(TimeWaitResponses));
                        wating.Start(crtime);

                        master.master.WriteSingleRegister(master.slaveID, TableUsedforRegisters.diagnostik_send, crtime);
                        
                        //logger.Trace("Master отправил сигналs");

                        have_connection = true;
                        Thread.Sleep(100);

                        ushort[] getrex = master.master.ReadHoldingRegisters(master.slaveID, TableUsedforRegisters.diagnostik_send, 1);
                        wating.Abort();

                        have_connection = true;
                        if (crtime == getrex[0])
                        {
                            crtime = (ushort)rand.Next(11, 100);
                            have_connection = true;
                            //logger.Trace("Обратная связь присутствует");
                        }
                        else
                        {
                            have_connection = false;
                            //logger.Trace("Обратная связь отсутствует");
                        }
                        crtime = (ushort)rand.Next(11, 100);

                        Thread.Sleep(deltatime);
                    }
                }
                catch (Exception ex)
                {
                    have_connection = false;
                    Stop();
                    have_connection = false;
                    Thread.Sleep(2000);

                    //logger.Trace("Cвязь Отсутствует");
                    //logger.Error(ex);
                    //Console.WriteLine(ex);
                    break;
                }
            }

            if (islive==false)
            {
                return;
            }
        }

        public void SlaveStart()
        {
            try
            {
                slave.Open();
            }
            catch(Exception ex)
            {

                //logger.Error(ex);
            }
            while (islive)
            {
               //Thread.Sleep(200);
               if (slave.try_reboot_connection == true)
               {
                    if (crtime != slave.randnumber)
                    {
                        crtime = slave.randnumber;
                        have_connection = true;
                        //logger.Trace("Есть связь между slave и master");
                        Thread.Sleep(deltatimeSlave);
                    }
                    else
                    {
                        crtime = slave.randnumber;
                        have_connection = false;
                        Thread.Sleep(deltatimeSlave);

                        //logger.Warn("Нету связи");

                    }
                }
            }
        }

        public void CloseManager()
        {
            islive = false;
        }

    }
}
