using System;

namespace CheckUserSession
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SessionManager sessionManager = new SessionManager();
            MessageReceiver receiver = new MessageReceiver(sessionManager, 5000);
            Console.WriteLine("Current Session ID: " + sessionManager.GetCurrentSessionId());
            receiver.Start();
        }
    }
}
