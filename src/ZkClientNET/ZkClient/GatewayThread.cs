using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient
{
    public class GatewayThread
    {
        public Thread gatewayThread;
        private static readonly ILog LOG = LogManager.GetLogger(typeof(GatewayThread));
        private int _port;
        private int _destinationPort;
        private int _lock = 0;
        private bool _running = false;

        Socket _serverSocket;

        public GatewayThread(int port, int destinationPort)
        {
            _port = port;
            _destinationPort = destinationPort;
            gatewayThread = new Thread(Run);
            gatewayThread.IsBackground = true;
        }

        public void Run()
        {
            LOG.Info("Starting gateway on port " + _port + " pointing to port " + _destinationPort);
            string host = "127.0.0.1";
            List<Thread> runningThreads = new List<Thread>();
            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(host), _port));

                if (0 == Interlocked.Exchange(ref _lock, 1))
                {
                    _running = true;
                }

                while (true)
                {
                    Socket socket = _serverSocket.Accept();
                    IPEndPoint ipEndClient = (IPEndPoint)socket.RemoteEndPoint;
                    LOG.Info(string.Format("new client is connected with {0} at port {1}", ipEndClient.Address, ipEndClient.Port));
                    Socket outgoingSocket;
                    try
                    {
                        outgoingSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        outgoingSocket.Connect(new IPEndPoint(IPAddress.Parse(host), _destinationPort));
                    }
                    catch (Exception e)
                    {
                        LOG.Warn("could not connect to " + _destinationPort);
                        continue;
                    }

                    Thread writeThread = new Thread(() =>
                    {
                        runningThreads.Add(Thread.CurrentThread);
                        try
                        {
                            byte[] recvBytes = new byte[1024];
                            int read = -1;
                            while ((read = socket.Receive(recvBytes, recvBytes.Length, SocketFlags.None)) != -1)
                            {
                                byte[] sendBytes = BitConverter.GetBytes(read);
                                outgoingSocket.Send(sendBytes, SocketFlags.None);
                            }
                        }
                        catch (SocketException)
                        {
                            // ignore
                        }
                        catch (ThreadInterruptedException)
                        {
                            try
                            {
                                socket.Close();
                                socket.Dispose();
                                outgoingSocket.Close();
                                outgoingSocket.Dispose();
                            }
                            catch (SocketException socketException)
                            {
                                LOG.Error("error on stopping closing sockets", socketException);
                            }
                            Thread.CurrentThread.Interrupt();
                        }
                        finally
                        {
                            CloseQuietly(outgoingSocket);
                            runningThreads.Remove(Thread.CurrentThread);
                        }
                    });

                    Thread readThread = new Thread(() =>
                    {
                        runningThreads.Add(Thread.CurrentThread);
                        try
                        {
                            byte[] recvBytes = new byte[1024];
                            int read = -1;
                            while ((read = outgoingSocket.Receive(recvBytes, recvBytes.Length, SocketFlags.None)) != -1)
                            {
                                byte[] sendBytes = BitConverter.GetBytes(read);
                                socket.Send(sendBytes, SocketFlags.None);
                            }
                        }
                        catch (SocketException)
                        {
                            // ignore
                        }
                        finally
                        {
                            CloseQuietly(socket);
                            runningThreads.Remove(Thread.CurrentThread);
                        }
                    });

                    writeThread.IsBackground = true;
                    readThread.IsBackground = true;
                    writeThread.Start();
                    readThread.Start();
                }
            }
            catch (SocketException e)
            {
                if (!_running)
                {
                    throw ExceptionUtil.ConvertToRuntimeException(e);
                }
                LOG.Info("Stopping gateway");
            }
            catch (ThreadInterruptedException)
            {
                try
                {
                    _serverSocket.Close();
                    _serverSocket.Dispose();
                }
                catch (Exception cE)
                {
                    LOG.Error("error on stopping gateway", cE);
                }
                Thread.CurrentThread.Interrupt();
            }
            catch (Exception e)
            {
                LOG.Error("error on gateway execution", e);
            }

            foreach (Thread thread in runningThreads)
            {
                thread.Interrupt();
                try
                {
                    thread.Join();
                }
                catch (ThreadInterruptedException)
                {
                    // ignore
                }
            }

        }

        public void Start()
        {
            gatewayThread.Start();
        }

        protected void CloseQuietly(Socket closable)
        {
            try
            {
                closable.Disconnect(true);
            }
            catch (SocketException e)
            {
                // ignore
            }
        }

        public void InterruptAndJoin()
        {
            gatewayThread.Interrupt();
            gatewayThread.Join();
        }

        public void AwaitUp()
        {
            Task.Factory.StartNew(() =>
            {
                while (!_running)
                {
                    Interlocked.Exchange(ref _lock, 0);
                }
            });        
        }
    }
}
