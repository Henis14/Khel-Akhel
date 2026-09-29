using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace Khel_Akhel_Server.BL.Common
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    public static class Logs
    {
        public static bool EnableLogs = true;

        public static LogLevel MinimumLevel = LogLevel.Debug;

        public static int LogRetentionDays = 45;
        public static long LogMaxFileSizeBytes = 100 * 1024 * 1024; // 100 MB safety threshold
        public static int LogRotationHours = 4;

        public static int MaxMessageLength = 8000;

        private const int QueueCapacity = 10_000;

        private static readonly object _fileLock = new();
        private static readonly Channel<string> _channel = Channel.CreateBounded<string>(
            new BoundedChannelOptions(QueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            }
        );

        private static long _droppedCount;
        private static readonly Task _writerTask;
        private static readonly CancellationTokenSource _cts = new();

        static Logs()
        {
            _writerTask = Task.Run(BackgroundWriteLoopAsync);
        }

        public static string GetLogFilePath(DateTime? now = null)
        {
            DateTime Now = now ?? DateTime.Now;
            string basePath = Directory.GetCurrentDirectory();

            string monthFolder = Now.ToString("yyyy-MM");
            string dateFolder = Now.ToString("yyyy-MM-dd");

            string folderPath = Path.Combine(basePath, "Logs", monthFolder, dateFolder);
            Directory.CreateDirectory(folderPath);

            int currentHour = Now.Hour;
            int windowStartHour = (currentHour / LogRotationHours) * LogRotationHours;
            int windowEndHour = windowStartHour + LogRotationHours;

            string windowLabel = $"{windowStartHour:D2}-{windowEndHour:D2}";
            string baseFileName = $"{dateFolder}_{windowLabel}_Logs";
            string baseFilePath = Path.Combine(folderPath, $"{baseFileName}.txt");

            if (!File.Exists(baseFilePath))
                return baseFilePath;

            FileInfo fileInfo = new(baseFilePath);
            if (fileInfo.Length < LogMaxFileSizeBytes)
                return baseFilePath;

            int sequence = 1;
            while (true)
            {
                string seqFilePath = Path.Combine(folderPath, $"{baseFileName}_{sequence:D3}.txt");
                if (!File.Exists(seqFilePath))
                    return seqFilePath;

                FileInfo seqFileInfo = new(seqFilePath);
                if (seqFileInfo.Length < LogMaxFileSizeBytes)
                    return seqFilePath;

                sequence++;
            }
        }

        public static void Debug(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0
        ) => Enqueue(LogLevel.Debug, message, filePath, memberName, lineNumber);

        public static void Info(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0
        ) => Enqueue(LogLevel.Info, message, filePath, memberName, lineNumber);

        public static void Warning(
            string message,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0
        ) => Enqueue(LogLevel.Warning, message, filePath, memberName, lineNumber);

        public static void Error(
            string message,
            Exception? ex = null,
            [CallerFilePath] string filePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            string fullMessage = ex != null ? message + Environment.NewLine + ex : message;
            Enqueue(LogLevel.Error, fullMessage, filePath, memberName, lineNumber);
        }

        private static void Enqueue(
            LogLevel level,
            string message,
            string filePath,
            string memberName,
            int lineNumber
        )
        {
            if (!EnableLogs || level < MinimumLevel)
                return;

            try
            {
                string fileName = string.IsNullOrWhiteSpace(filePath)
                    ? "Unknown"
                    : Path.GetFileName(filePath);
                string safeMessage = Sanitize(message);

                string log =
                    $"{DateTime.Now:yyyy-MM-dd hh:mm:ss.fff tt}  | "
                    + $"[{level.ToString().ToUpperInvariant()}] | "
                    + $"[{fileName}:{lineNumber}] | "
                    + $"[{memberName}] | "
                    + safeMessage;

                if (!_channel.Writer.TryWrite(log))
                {
                    Interlocked.Increment(ref _droppedCount);
                }
            }
            catch (Exception ex)
            {
                SafeConsoleError(
                    $"[Logs Failure Fallback] Unable to enqueue log entry: {ex.Message}"
                );
            }
        }

        private static string Sanitize(string? message)
        {
            if (string.IsNullOrEmpty(message))
                return string.Empty;

            if (message.Length > MaxMessageLength)
                message =
                    message[..MaxMessageLength] + $"...[truncated, {message.Length} chars total]";

            var sb = new StringBuilder(message.Length);
            foreach (char c in message)
            {
                if (c == '\r' || c == '\n')
                    sb.Append(" \\n ");
                else if (char.IsControl(c) && c != '\t')
                    continue; // drop other control chars
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        private static async Task BackgroundWriteLoopAsync()
        {
            var reader = _channel.Reader;
            long lastDroppedReported = 0;

            try
            {
                while (await reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out string? entry))
                    {
                        WriteToFile(entry);
                    }

                    long dropped = Interlocked.Read(ref _droppedCount);
                    if (dropped != lastDroppedReported)
                    {
                        WriteToFile(
                            $"{DateTime.Now:yyyy-MM-dd hh:mm:ss.fff tt} | [WARNING] | [Logs.cs] | [BackgroundWriteLoopAsync] | "
                                + $"{dropped - lastDroppedReported} log entries dropped due to full queue since last report."
                        );
                        lastDroppedReported = dropped;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                SafeConsoleError(
                    $"[Logs Failure Fallback] Background writer crashed: {ex.Message}"
                );
            }

            while (reader.TryRead(out string? entry))
            {
                WriteToFile(entry);
            }
        }

        private static void WriteToFile(string log)
        {
            try
            {
                lock (_fileLock)
                {
                    string targetFilePath = GetLogFilePath();
                    File.AppendAllText(targetFilePath, log + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                SafeConsoleError(
                    $"[Logs Failure Fallback] Unable to write log entry: {ex.Message}"
                );
            }
        }

        private static void SafeConsoleError(string message)
        {
            try
            {
                Console.Error.WriteLine(message);
            }
            catch { }
        }

        public static void Shutdown(TimeSpan? timeout = null)
        {
            try
            {
                _channel.Writer.TryComplete();
                _writerTask.Wait(timeout ?? TimeSpan.FromSeconds(5));
            }
            catch { }
        }

        public static void CleanupOldLogs(int daysToKeep = 45)
        {
            try
            {
                string logsPath = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                if (!Directory.Exists(logsPath))
                    return;

                DateTime cutoffDate = DateTime.Now.Date.AddDays(-daysToKeep);

                lock (_fileLock)
                {
                    string[] monthDirectories = Directory.GetDirectories(logsPath);
                    foreach (string monthDir in monthDirectories)
                    {
                        string[] dayDirectories = Directory.GetDirectories(monthDir);
                        foreach (string dayDir in dayDirectories)
                        {
                            string dayFolderName = Path.GetFileName(dayDir);

                            if (
                                DateTime.TryParse(dayFolderName, out DateTime folderDate)
                                && folderDate.Date < cutoffDate
                            )
                            {
                                try
                                {
                                    Directory.Delete(dayDir, true);
                                }
                                catch (Exception ex)
                                {
                                    SafeConsoleError(
                                        $"[Logs Cleanup Error] Could not delete directory {dayDir}: {ex.Message}"
                                    );
                                }
                            }
                        }

                        if (Directory.GetFileSystemEntries(monthDir).Length == 0)
                        {
                            try
                            {
                                Directory.Delete(monthDir);
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SafeConsoleError($"[Logs Cleanup Error] CleanupOldLogs failed: {ex.Message}");
            }
        }
    }
}
