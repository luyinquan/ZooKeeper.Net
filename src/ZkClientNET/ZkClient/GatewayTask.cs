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
    public class GatewayTask
    {
        public Task gatewayTask;
        private static readonly ILog LOG = LogManager.GetLogger(typeof(GatewayTask));
        private int _port;
        private int _destinationPort;
        private int _lock = 0;
        private bool _running = false;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        Socket _serverSocket;

        public GatewayTask(int port, int destinationPort)
        {
            _port = port;
            _destinationPort = destinationPort;
            gatewayTask = new Task(Run, tokenSource.Token);
        }

        public void Run()
        {
            while (!tokenSource.IsCancellationRequested)
            {
                LOG.Info("Starting gateway on port " + _port + " pointing to port " + _destinationPort);
                string host = "127.0.0.1";
                try
                {
                    _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(host), _port));

                    if (0 == Interlocked.Exchange(ref _lock, 1))
                    {
                        _running = true;
                    }

                    #region while
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

                        CancellationTokenSource writeTokenSource = new CancellationTokenSource();
                        Task writeTask = new Task(() =>
                        {
                            try
                            {
                                byte[] recvBytes = new byte[1024];
                                int read = -1;
                                while (!writeTokenSource.IsCancellationRequested &&
                                (read = socket.Receive(recvBytes, recvBytes.Length, SocketFlags.None)) != -1)
                                {
                                    byte[] sendBytes = BitConverter.GetBytes(read);
                                    outgoingSocket.Send(sendBytes, SocketFlags.None);
                                }
                            }
                            catch (SocketException)
                            {
                                // ignore
                            }
                            finally
                            {
                                CloseQuietly(outgoingSocket);
                            }

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
                        }
                        , writeTokenSource.Token);

                        CancellationTokenSource readTokenSource = new CancellationTokenSource();
                        Task readTask = new Task(() =>
                        {
                            try
                            {
                                byte[] recvBytes = new byte[1024];
                                int read = -1;
                                while (!readTokenSource.IsCancellationRequested &&
                                (read = outgoingSocket.Receive(recvBytes, recvBytes.Length, SocketFlags.None)) != -1)
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
                            }
                        }, readTokenSource.Token);

                        writeTask.Start();
                        try
                        {
                            writeTask.Wait();
                        }
                        catch (AggregateException e)
                        {
                            writeTokenSource.Cancel();
                        }

                        readTask.Start();
                        try
                        {
                            readTask.Wait();
                        }
                        catch (AggregateException e)
                        {
                            readTokenSource.Cancel();
                        }
                    } 
                    #endregion
                }
                catch (SocketException e)
                {
                    if (!_running)
                    {
                        throw ExceptionUtil.ConvertToRuntimeException(e);
                    }
                    LOG.Info("Stopping gateway");
                }
                catch (Exception e)
                {
                    LOG.Error("error on gateway execution", e);
                }
            }

            try
            {
                _serverSocket.Close();
                _serverSocket.Dispose();
            }
            catch (Exception cE)
            {
                LOG.Error("error on stopping gateway", cE);
            }
        }

        public void Start()
        {
            gatewayTask.Start();
        }

        public void ReSet()
        {
            tokenSource = new CancellationTokenSource();
            gatewayTask = new Task(Run, tokenSource.Token);
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

        public void CancelAndWait()
        {
            tokenSource.Cancel();
            gatewayTask.Wait();
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
