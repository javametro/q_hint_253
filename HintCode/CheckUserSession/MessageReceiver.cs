using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CheckUserSession
{
    public class MessageReceiver
    {
        private SessionManager _sessionManager;
        private TcpListener _listener;

        public MessageReceiver(SessionManager sessionManager, int port)
        {
            _sessionManager = sessionManager;
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine("Server started. Waiting for connections...");

            while (true)
            {
                var client = _listener.AcceptTcpClient();
                var stream = client.GetStream();

                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                string[] parts = message.Split('|');
                int sessionId = int.Parse(parts[0]);
                string content = parts[1];

                if (_sessionManager.ValidateSession(sessionId))
                {
                    Console.WriteLine("Message accepted: " + content);
                }
                else
                {
                    Console.WriteLine("Message refused: Invalid session");
                }

                stream.Close();
                client.Close();
            }
        }
    }
}
