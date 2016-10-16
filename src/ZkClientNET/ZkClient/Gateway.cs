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
        //private GatewayThread _thread;
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
            //if (_thread != null)
            //{
            //    throw new ArgumentNullException("Gateway already running");
            //}
            //_thread = new GatewayThread(_port, _destinationPort);
            //_thread.Start();
            //_thread.AwaitUp();

            if (_task != null)
            {
                throw new ArgumentNullException("Gateway already running");
            }
            _task = new GatewayTask(_port, _destinationPort);
            _task.Start();
            _task.AwaitUp();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            //if (_thread != null)
            //{
            //    try
            //    {
            //        _thread.InterruptAndJoin();
            //    }
            //    catch (ThreadInterruptedException e)
            //    {
            //        Thread.CurrentThread.Interrupt();
            //    }
            //    _thread = null;
            //}

            if (_task != null)
            {
                _task.CancelAndWait();
                _task = null;
            }
        }
    }
}
