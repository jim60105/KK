namespace Extension {
    public class Logger {
        public static object logger { get => _logger; set => _logger = value; }

        private static object _logger;

        public static void LogDebug(string data) => _logger?.Invoke("LogDebug", new object[] { data });
        public static void LogWarning(string data) => _logger?.Invoke("LogWarning", new object[] { data });
        public static void LogError(string data) => _logger?.Invoke("LogError", new object[] { data });
    }
}
