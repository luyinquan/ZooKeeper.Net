using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient
{
    public class Gateway
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(Gateway));
        private int _port;
        private int _destinationPort;
        private int _lock = 0;
        private bool _running = false;

        Socket _serverSocket;

        public Gateway(int port, int destinationPort)
        {
            _port = port;
            _destinationPort = destinationPort;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start()
        {
            Task.Factory.StartNew(() =>
            {
                LOG.Info("Starting gateway on port " + _port + " pointing to port " + _destinationPort);
                List<Thread> runningThreads = new List<Thread>();
                string host = "127.0.0.1";
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(host), _port));

                _running = true;

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
                        catch (SocketException socketException)
                        {
                            // ignore
                        }
                        catch (ThreadInterruptedException threadInterruptedException)
                        {
                            try
                            {
                                socket.Close();
                                outgoingSocket.Close();

                            }
                            catch (SocketException socketException)
                            {
                                LOG.Error("error on stopping closing sockets", e);
                            }
                            Thread.CurrentThread.Interrupt();
                        }
                        finally
                        {
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
                            while ((read = outgoingSocket.Receive(recvBytes, recvBytes.Length, , SocketFlags.None)) != -1)
                            {
                                byte[] sendBytes = BitConverter.GetBytes(read);
                                socket.Send(sendBytes, SocketFlags.None);
                            }
                        }
                        catch (IOException e)
                        {
                            // ignore
                        }
                        finally
                        {
                            runningThreads.Remove(Thread.CurrentThread);
                        }
                    });

                    writeThread.IsBackground = true;
                    readThread.IsBackground = true;
                    writeThread.Start();
                    readThread.Start();
                }
            });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {

        }

    }
}
