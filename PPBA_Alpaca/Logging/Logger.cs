using PPBA_Alpaca.AppConfig;
using PPBA_Alpaca.Properties;
using System;
using System.IO;
using System.Threading;

namespace PPBA_Alpaca.Logging
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3
    }

    public class Logger : IDisposable
    {
        private readonly string _source;
        private readonly LogLevel _minLevel;
        private readonly StreamWriter _writer;
        private readonly object _sync = new object();
        private bool _disposed;

        public Logger(string source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _minLevel = PPBA_Alpaca.AppConfig.Settings.Default.MinLogLevel;


            // Ensure logs directory exists
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);

            // Daily log file named yyyyMMdd.log
            var filename = $"{DateTime.Now:yyyyMMdd}.log";
            var filepath = Path.Combine(logDir, filename);

            // Open for append, shared read
            var fs = new FileStream(filepath, FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(fs) { AutoFlush = true };
        }

        public void LogDebug(string message) => Log(LogLevel.Debug, message, null);
        public void LogInfo(string message) => Log(LogLevel.Info, message, null);
        public void LogWarn(string message) => Log(LogLevel.Warn, message, null);
        public void LogError(string message, Exception ex) => Log(LogLevel.Error, message, ex);

        private void Log(LogLevel level, string message, Exception ex)
        {
            if (level < _minLevel)
                return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var lvl = level.ToString().ToUpper();

            // Compose the line
            var line = $"{timestamp} [{threadId}] {lvl}/{_source}: {message}";
            if (ex != null)
            {
                line += $"{Environment.NewLine}    ↳ {ex.GetType().FullName}: {ex.Message}"
                      + $"{Environment.NewLine}{ex.StackTrace}";
            }

            try
            {
                lock (_sync)
                {
                    _writer.WriteLine(line);
                }

                // Mirror to console for operator feedback
                Console.WriteLine(line);
            }
            catch
            {
                // Swallow any logging failures to avoid recursive exceptions
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                _writer.Dispose();
                _disposed = true;
            }
        }
    }
}
