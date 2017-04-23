using log4net;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ZookeeperClient.Client
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

            public Func<Task>  Run { set; get; }

            public ZKEvent(string description)
            {
                this.description = description;
            }
       
            public override string ToString()
            {
                return $"ZKEvent[{description}]";
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

        public  void Run()
        {
            while (!tokenSource.IsCancellationRequested)
            {
                LOG.Info("Starting ZookeeperClient event thread.");
                ZKEvent zkEvent = _events.Take();
                Interlocked.Increment(ref _eventId);
                int eventId = _eventId;
                LOG.Debug($"Delivering event #{eventId}{zkEvent.ToString()}" );
                try
                {
                    zkEvent.Run();
                }
                catch (Exception e)
                {
                    LOG.Error($"Error handling event {zkEvent.ToString()}", e);
                }
                LOG.Debug($"Delivering event #{eventId} done");
            }
        }

        public void Send(ZKEvent @event)
        {
            if (_zkTask != null && _zkTask.Status != TaskStatus.Canceled)
            {
                LOG.Debug($"New event: {@event.ToString()}");
                _events.Add(@event);
            }
        }

        public void Cancel()
        {
            tokenSource.Cancel();
        }

    }
}

