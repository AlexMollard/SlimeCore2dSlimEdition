namespace SlimeCore
{
    public static class Logger
    {
        public static void Trace(string message)
        {
            SafeNativeMethods.Engine_LogTrace(message);
        }

        public static void Info(string message)
        {
            SafeNativeMethods.Engine_LogInfo(message);
        }

        public static void Warn(string message)
        {
            SafeNativeMethods.Engine_LogWarn(message);
        }

        public static void Error(string message)
        {
            SafeNativeMethods.Engine_LogError(message);
        }

        public static void Trace(object message) => Trace(message?.ToString() ?? "null");
        public static void Info(object message) => Info(message?.ToString() ?? "null");
        public static void Warn(object message) => Warn(message?.ToString() ?? "null");
        public static void Error(object message) => Error(message?.ToString() ?? "null");
    }
}
