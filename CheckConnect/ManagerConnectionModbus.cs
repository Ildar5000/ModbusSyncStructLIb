using ModbusSyncStructLIb.DespriptionState;
using ModbusSyncStructLIb.EvenBase;
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
        public QueueOfSentMessagesForSlave queueOf;

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
                logger.Warn("Менедженр соединения: Slave не отвечает в течение нескольких секунд");
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
                Thread.Sleep(timeRecoveraftercrash);
                //logger.Error(ex);
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
                    logger.Error(ex);
                    logger.Error("Менедженр соединения: Не удалось подключиться к Slave, попытка связаться через"+ timeRecoveraftercrash);
                    Thread.Sleep(timeRecoveraftercrash);
                    Stop();
                    var sendfile = Task.Run(() => Restart());
                    

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
            ///Если нет связи
            int count_time = 0;
            try
            {
                slave.Open();
            }
            catch(Exception ex)
            {
                logger.Error(ex);
                logger.Error("Менеджер соединения: Ошибка при создании Modbus Slave");
            }
            while (islive)
            {
               if (slave.try_reboot_connection == true)
               {
                    if (crtime != slave.randnumber)
                    {
                        crtime = slave.randnumber;
                        have_connection = true;
                        count_time = 0;
                        Thread.Sleep(deltatimeSlave+300);
                    }
                    else
                    {
                        crtime = slave.randnumber;
                        have_connection = false;
                        Thread.Sleep(deltatimeSlave+ 300);
                        count_time++;
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
