using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZkClientNET.ZkClient.Exceptions;

namespace ZkClientNET.ZkClient
{
   
    public class ZkEventThread
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZkEventThread));

        public Thread _zkEventThread;

        private BlockingCollection<ZkEvent> _events = new BlockingCollection<ZkEvent>(new ConcurrentQueue<ZkEvent>());

        private int _eventId = 0;

        public class ZkEvent
        {
            private string description { set; get; }

            public Action Run { set; get; }

            public ZkEvent(string description)
            {
                this.description = description;
            }

          

            public override string ToString()
            {
                return "ZkEvent[" + description + "]";
            }
        }

        public ZkEventThread(string name)
        {
            _zkEventThread = new Thread(Run);
            _zkEventThread.IsBackground = true;
            _zkEventThread.Name = "ZkClient-EventThread-" + Thread.CurrentThread.ManagedThreadId + "-" + name;
        }

        public void Start()
        {
            _zkEventThread.Start();
        }

        public void Run()
        {
            LOG.Info("Starting ZkClient event thread.");
            try
            {
                while (Thread.CurrentThread.ThreadState != ThreadState.WaitSleepJoin)
                {
                    ZkEvent zkEvent = _events.Take();
                    Interlocked.Increment(ref _eventId);
                    int eventId = _eventId;
                    LOG.Debug("Delivering event #" + eventId + " " + zkEvent);
                    try
                    {
                        zkEvent.Run();
                    }
                    catch (ThreadInterruptedException e)
                    {
                        Thread.CurrentThread.Interrupt();
                    }
                    catch (ZkInterruptedException e)
                    {
                        Thread.CurrentThread.Interrupt();
                    }
                    catch (Exception e)
                    {
                        LOG.Error("Error handling event " + zkEvent, e);
                    }
                    LOG.Debug("Delivering event #" + eventId + " done");
                }
            }
            catch (ThreadInterruptedException e)
            {
                LOG.Info("Terminate ZkClient event thread.");
            }
        }

        public void Send(ZkEvent @event)
        {
            if (_zkEventThread.ThreadState != ThreadState.WaitSleepJoin)
            {
                LOG.Debug("New event: " + @event);
                _events.Add(@event);
            }
        }

        public void Interrupt()
        {
            _zkEventThread.Interrupt();
        }

        public void Join(int millisecondsTimeout)
        {
            _zkEventThread.Join(millisecondsTimeout);
        }
    }
}

