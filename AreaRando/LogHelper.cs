// ReSharper disable file UnusedMember.Global

namespace AreaRando
{
    public static class LogHelper
    {
        public static void Log(string message)
        {
            AreaRando.Instance.Log(message);
        }

        public static void Log(object message)
        {
            AreaRando.Instance.Log(message);
        }

        public static void LogDebug(string message)
        {
            AreaRando.Instance.LogDebug(message);
        }

        public static void LogDebug(object message)
        {
            AreaRando.Instance.LogDebug(message);
        }

        public static void LogError(string message)
        {
            AreaRando.Instance.LogError(message);
        }

        public static void LogError(object message)
        {
            AreaRando.Instance.LogError(message);
        }

        public static void LogFine(string message)
        {
            AreaRando.Instance.LogFine(message);
        }

        public static void LogFine(object message)
        {
            AreaRando.Instance.LogFine(message);
        }

        public static void LogWarn(string message)
        {
            AreaRando.Instance.LogWarn(message);
        }

        public static void LogWarn(object message)
        {
            AreaRando.Instance.LogWarn(message);
        }
    }
}