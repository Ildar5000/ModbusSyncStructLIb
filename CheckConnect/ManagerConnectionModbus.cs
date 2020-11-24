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
        public int timeRecoveraftercrash = 30 * 1000;

        Logger logger;

        bool islive = true;
        public bool have_connection = false;

        Random rand = new Random();

        ushort crtime = 0;

        public ManagerConnectionModbus(MasterSyncStruct master)
        {
            logger = LogManager.GetCurrentClassLogger();

            deltatime = master.deltaTimeCheck;
            deltatimeSlave = deltatime;
            timeRecoveraftercrash = master.timeRecoveraftercrash;

            this.master = master;
            crtime = 0;
        }

        public ManagerConnectionModbus(SlaveSyncSruct slave)
        {
            logger = LogManager.GetCurrentClassLogger();
            deltatime = slave.deltaTimeCheck;
            deltatimeSlave = deltatime + 100;
            timeRecoveraftercrash = slave.timeRecoveraftercrash;
            this.slave = slave;
            crtime = 0;
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
            crtime = 0;
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

        public void TimeWaitResponses(ushort date, bool stopthread)
        {
            ushort dateold = (ushort)date;

            bool stopthreadstop = (bool)stopthread;

            Thread.Sleep(110);
            if (stopthreadstop==true)
            {
                return;
            }

            Thread.Sleep(timeRecoveraftercrash);
            if (dateold== crtime)
            {
                logger.Warn("Не отвечает Slave в течение нескольких секунд");
                //Stop();
                //Restart();
                return;
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
                        bool stopthread = false;
                        master.master.WriteSingleRegister(master.slaveID, TableUsedforRegisters.diagnostik_send, crtime);

                        //Thread wating = new Thread(new ParameterizedThreadStart(TimeWaitResponses));
                        //wating.Start(crtime, stopthread);

                        var sendfile = Task.Run(() => TimeWaitResponses(crtime, stopthread));


                        //logger.Trace("Master отправил сигналs");

                        have_connection = true;
                        Thread.Sleep(100);

                        ushort[] getrex = master.master.ReadHoldingRegisters(master.slaveID, TableUsedforRegisters.diagnostik_send, 1);
                        
                        stopthread = true;


                        have_connection = true;
                        if (crtime == getrex[0])
                        {
                            have_connection = true;
                        }
                        else
                        {
                            have_connection = false;
                        }

                        if (crtime== 65000)
                        {
                            crtime = 0;
                        }

                        crtime++;

                        Thread.Sleep(deltatime-100);
                    }
                }
                catch (Exception ex)
                {
                    have_connection = false;
                    Thread.Sleep(timeRecoveraftercrash);
                    Stop();
                    Restart();
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
               if (slave.try_reboot_connection == true)
               {
                    if (crtime != slave.randnumber)
                    {
                        crtime = slave.randnumber;
                        have_connection = true; 
                        Thread.Sleep(deltatimeSlave+300);
                    }
                    else
                    {
                        crtime = slave.randnumber;
                        have_connection = false;
                        Thread.Sleep(deltatimeSlave+ 300);
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
