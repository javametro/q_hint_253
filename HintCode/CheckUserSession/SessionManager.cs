using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CheckUserSession
{
    public class SessionManager
    {
        private int _currentSessionId;

        public SessionManager()
        {
            _currentSessionId = GetCurrentSessionId();
        }

        public int GetCurrentSessionId()
        {
            // Get the current user's session ID
            int sessionId = Process.GetCurrentProcess().SessionId;
            return sessionId;
        }

        public bool ValidateSession(int sessionId)
        {
            // Get the active console session ID (not just the process session ID)
            int activeSessionId = GetActiveConsoleSessionId();

            // Compare the provided session ID with the active console session ID
            return sessionId == activeSessionId;
        }

        [DllImport("kernel32.dll")]
        private static extern int WTSGetActiveConsoleSessionId();

        private int GetActiveConsoleSessionId()
        {
            return WTSGetActiveConsoleSessionId();
        }
    }
}
