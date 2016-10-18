using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;


namespace ZkClientNET.ZkClient
{
    public class Gateway
    {
        private GatewayTask _task;
        private int _port;
        private int _destinationPort;

        public Gateway(int port, int destinationPort)
        {
            _port = port;
            _destinationPort = destinationPort;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start()
        {
            if (_task != null)
            {
                throw new Exception("Gateway already running");
            }
            _task = new GatewayTask(_port, _destinationPort);
            _task.Start();       
            _task.AwaitUp();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            if (_task != null)
            {
                _task.CancelAndWait();
                _task = null;
            }
        }
    }
}
