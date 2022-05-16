using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    public class Server
    {
        private const int ServiceTime = 5000;

        private static byte[] _buffer = new byte[1024];
        private static List<Socket> _clientSockets = new List<Socket>();
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static object locker = new object();
        private static int _serviceCount = 0;
        private static int _timeSpent = 0;

        public Server()
        {

        }

        public void SetupServer()
        {
            Console.WriteLine("Setting up server...");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 100));
            _serverSocket.Listen(5);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            Socket socket = _serverSocket.EndAccept(AR);
            _clientSockets.Add(socket);
            Console.WriteLine("Client connected");
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            bool acquiredLock = false;
            try
            {
                Socket socket = (Socket)AR.AsyncState;
                if (_serviceCount == 0)
                {
                    SendMessage(socket, $"Pending for service. Time until next status: 0 ms");
                }
                else
                {
                    SendMessage(socket, $"Pending for service. Time until next status: {(_serviceCount - 1) * ServiceTime + ServiceTime - _timeSpent} ms");
                }

                int received = socket.EndReceive(AR);
                byte[] dataBuf = new byte[received];
                Array.Copy(_buffer, dataBuf, received);
                string text = Encoding.ASCII.GetString(dataBuf);
                Console.WriteLine($"Text received: {text}");

                _serviceCount++;
                Monitor.Enter(locker, ref acquiredLock);
                SendMessage(socket, $"Service started...{Environment.NewLine}Time until next status: {ServiceTime} ms");

                for (_timeSpent = 0; _timeSpent < ServiceTime; _timeSpent += 100)
                {
                    Thread.Sleep(100);
                }
                string response = String.Empty;
                if (text.ToLower() != "get time")
                {
                    response = "Invalid request";
                }
                else
                {
                    response = DateTime.Now.ToLongTimeString();
                }
                SendMessage(socket, response);
                SendMessage(socket, "OK");
                _serviceCount--;

                socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            }
            finally
            {
                if (acquiredLock) Monitor.Exit(locker);
            }
        }

        private void SendMessage(Socket socket, string message)
        {
            byte[] data = Encoding.ASCII.GetBytes(message);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
        }

        private void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }
    }
}
