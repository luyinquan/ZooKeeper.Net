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
   
    public class ZkTask
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZkTask));

        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public Task _zkTask;

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

        public ZkTask(string name)
        {
            tokenSource = new CancellationTokenSource();
            _zkTask = new Task(Run, tokenSource.Token);
        }

        public void ReSet()
        {
            tokenSource = new CancellationTokenSource();
            _zkTask = new Task(Run, tokenSource.Token);
        }

        public void Start()
        {
            _zkTask.Start();
        }

        public void Run()
        {
            while (!tokenSource.IsCancellationRequested)
            {
                LOG.Info("Starting ZkClient event thread.");
                ZkEvent zkEvent = _events.Take();
                Interlocked.Increment(ref _eventId);
                int eventId = _eventId;
                LOG.Debug("Delivering event #" + eventId + " " + zkEvent);
                try
                {
                    zkEvent.Run();
                }
                catch (ZkInterruptedException e)
                {
                    tokenSource.Cancel();
                }
                catch (Exception e)
                {
                    LOG.Error("Error handling event " + zkEvent, e);
                }
                LOG.Debug("Delivering event #" + eventId + " done");
            }
        }

        public void Send(ZkEvent @event)
        {
            if (_zkTask.Status != TaskStatus.Canceled)
            {
                LOG.Debug("New event: " + @event);
                _events.Add(@event);
            }
        }

        public void Cancel()
        {
            tokenSource.Cancel();
        }

        public void Wait(int millisecondsTimeout)
        {
            _zkTask.Wait(millisecondsTimeout);
        }

    }
}

