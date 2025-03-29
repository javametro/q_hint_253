using System;
using System.Net.Sockets;
using System.Text;

namespace CheckUserSessionClient
{
    public class Client
    {
        static void Main(string[] args)
        {
            Console.Write("Enter server IP: ");
            string serverIP = Console.ReadLine();

            Console.Write("Enter session ID: ");
            int sessionId = int.Parse(Console.ReadLine());

            Console.Write("Enter message: ");
            string message = Console.ReadLine();

            SendMessage(serverIP, sessionId, message);
        }

        static void SendMessage(string serverIP, int sessionId, string message)
        {
            try
            {
                TcpClient client = new TcpClient(serverIP, 5000);
                NetworkStream stream = client.GetStream();

                string fullMessage = sessionId + "|" + message;
                byte[] data = Encoding.UTF8.GetBytes(fullMessage);

                stream.Write(data, 0, data.Length);
                Console.WriteLine("Message sent.");

                stream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
