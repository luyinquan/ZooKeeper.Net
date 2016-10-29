using log4net;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ZKClientNET.Exceptions;

namespace ZKClientNET.Client
{

    public class ZKTask
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZKTask));

        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public Task _zkTask;

        private BlockingCollection<ZKEvent> _events = new BlockingCollection<ZKEvent>(new ConcurrentQueue<ZKEvent>());

        private int _eventId = 0;

        public class ZKEvent
        {
            private string description { set; get; }

            public Action Run { set; get; }

            public ZKEvent(string description)
            {
                this.description = description;
            }
       
            public override string ToString()
            {
                return "ZKEvent[" + description + "]";
            }
        }

        public ZKTask(string name)
        {
            tokenSource = new CancellationTokenSource();
            _zkTask = new Task(Run, tokenSource.Token);
        }
  
        public void Start()
        {
            _zkTask.Start();
        }

        public void ReSet()
        {
            tokenSource = new CancellationTokenSource();
            _zkTask = new Task(Run, tokenSource.Token);
        }

        public void Run()
        {
            while (!tokenSource.IsCancellationRequested)
            {
                LOG.Info("Starting ZKClient event thread.");
                ZKEvent zkEvent = _events.Take();
                Interlocked.Increment(ref _eventId);
                int eventId = _eventId;
                LOG.Debug("Delivering event #" + eventId + " " + zkEvent);
                try
                {
                    zkEvent.Run();
                }
                catch (ZKInterruptedException e)
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

        public void Send(ZKEvent @event)
        {
            if (_zkTask != null && _zkTask.Status != TaskStatus.Canceled)
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

