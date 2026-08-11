using MLGWorks.Utils.Patterns;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace MLGWorks.Utils.Logging
{
    /// <summary>Specifies the root location used for log files.</summary>
    public enum LogLocationType { PersistentDataPath, DataPath, Custom }

    /// <summary>Specifies the severity or category of a log entry.</summary>
    public enum LogLevel
    {
        Debug = 0, Info = 1, Warning = 2, Error = 3, Command = 4, Output = 5, Custom = 6
    }

    
    /// <summary>
    /// Queues application and Unity log messages, writes them to configured files,
    /// and optionally mirrors application messages to the Unity console.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class Logger : Singleton<Logger>
    {
        [Header("Log File Location")]
        public LogLocationType pathType = LogLocationType.PersistentDataPath;
        public string relativePath = "Logs";
        public string customPath = "";

        [Header("Logging Options")]
        public bool enableDebugLogging = true;
        public bool logToUnityConsole = true;

        [Tooltip("-1 = keep all files")]
        public int maxLogFileCount = 10;

        [Header("Log File Format")]
        [Tooltip("Default extension for log files (include the dot)")]
        public string fileExtension = ".log";

        [Header("Log Level → File Targets")]
        [Tooltip("Bitmask: 0=Debug file, 1=Info file, 2=Warning file, 3=Error file, 4=Combined file")]
        public int debugTargets = (1 << 0) | (1 << 4);
        public int infoTargets = (1 << 1) | (1 << 4);
        public int warningTargets = (1 << 2) | (1 << 4);
        public int errorTargets = (1 << 3) | (1 << 4);

        /// <summary>Gets the fully resolved directory in which this logger writes files.</summary>
        /// <exception cref="ArgumentException">Thrown when the configured path is invalid.</exception>
        public string LogDirectory => Path.Combine(
            pathType switch
            {
                LogLocationType.DataPath => Application.dataPath,
                LogLocationType.Custom => customPath,
                _ => Application.persistentDataPath
            }, relativePath);

        /// <summary>Represents one timestamped message handled by the logger.</summary>
        public struct LogEntry
        {
            /// <summary>Gets or sets the time at which the message was queued.</summary>
            public DateTime Timestamp;
            /// <summary>Gets or sets the severity or category of the message.</summary>
            public LogLevel Level;
            /// <summary>Gets or sets the message text.</summary>
            public string Message;
            internal bool EchoToUnity;
        }

        private readonly ConcurrentQueue<LogEntry> logQueue = new();
        private readonly Dictionary<int, StreamWriter> writers = new();
        private readonly object lifecycleLock = new();
        private int initialized;
        private int handlingUnityLog;
        private bool hookSubscribed;
        private bool isShuttingDown;

        public event Action<List<LogEntry>> OnNewLogBatch;

        /// <summary>Initializes the singleton identity when Unity awakens the component.</summary>
        protected override void Awake() => base.Awake();

        /// <summary>Ensures that the file writers and Unity callback are initialized.</summary>
        /// <exception cref="IOException">Thrown when the log directory or files cannot be created.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the process cannot access the log path.</exception>
        private void EnsureInitialized()
        {
            if (Volatile.Read(ref initialized) == 0)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Creates the configured log files, subscribes to Unity log notifications,
        /// and prunes older log sessions. Initialization is performed at most once.
        /// </summary>
        /// <exception cref="IOException">Thrown when the log directory cannot be created.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the log path cannot be accessed.</exception>
        private void Initialize()
        {
            lock (lifecycleLock)
            {
                if (Volatile.Read(ref initialized) != 0 || isShuttingDown)
                {
                    return;
                }

                Directory.CreateDirectory(LogDirectory);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

                var needed = new HashSet<int>();
                MarkTargets(needed, debugTargets);
                MarkTargets(needed, infoTargets);
                MarkTargets(needed, warningTargets);
                MarkTargets(needed, errorTargets);

                foreach (int target in needed)
                {
                    string name = target switch
                    {
                        0 => "debug",
                        1 => "info",
                        2 => "warning",
                        3 => "error",
                        4 => "combined",
                        _ => "unknown"
                    };

                    string filename = $"{timestamp}_{name}{fileExtension}";
                    try
                    {
                        writers[target] = new StreamWriter(
                            Path.Combine(LogDirectory, filename), append: true) { AutoFlush = true };
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Logger failed to create '{filename}': {ex}");
                    }
                }

                Volatile.Write(ref initialized, 1);
                SubscribeToUnityLogs();
                CleanupOldLogFiles();
            }
        }

        /// <summary>Adds the file target bits contained in a log routing mask.</summary>
        /// <param name="targets">The set to which selected target indexes are added.</param>
        /// <param name="mask">The bitmask describing the selected files.</param>
        private static void MarkTargets(HashSet<int> targets, int mask)
        {
            for (int bit = 0; bit < 5; bit++)
            {
                if ((mask & (1 << bit)) != 0)
                {
                    targets.Add(bit);
                }
            }
        }

        /// <summary>Emits one test message for each supported standard log level.</summary>
        public static void EmitTestLogs()
        {
            Debug("This is a test Debug log.");
            Info("This is a test Info log.");
            Warning("This is a test Warning log.");
            Error("This is a test Error log.");
        }

        /// <summary>
        /// Receives a Unity log notification and queues it for later processing.
        /// This method is deliberately limited to thread-safe translation and queueing
        /// because Unity may invoke it from a worker thread.
        /// </summary>
        /// <param name="condition">The Unity log message.</param>
        /// <param name="stackTrace">The associated stack trace, if available.</param>
        /// <param name="type">The Unity log type used to map the message to a log level.</param>
        private void UnityLogHook(string condition, string stackTrace, LogType type)
        {
            // This callback can execute on a worker thread. It must only translate the
            // message and enqueue it; initialization, Unity API calls and file I/O stay
            // on the normal Update/shutdown path.
            if (Volatile.Read(ref handlingUnityLog) != 0 || isShuttingDown ||
                Volatile.Read(ref initialized) == 0)
            {
                return;
            }

            LogLevel level = type switch
            {
                LogType.Warning => LogLevel.Warning,
                LogType.Error or LogType.Exception => LogLevel.Error,
                _ => LogLevel.Info
            };

            string message = "[UNITY] " + condition;
            if (type == LogType.Error || type == LogType.Exception)
            {
                message += "\n" + stackTrace;
            }

            logQueue.Enqueue(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                EchoToUnity = false
            });
        }

        /// <summary>Flushes queued entries during Unity's main-thread update loop.</summary>
        private void Update() => Flush();

        /// <summary>Detaches callbacks and closes writers when the component is destroyed.</summary>
        protected override void OnDestroy()
        {
            Shutdown();
            base.OnDestroy();
        }

        /// <summary>Performs an orderly shutdown when the Unity application quits.</summary>
        private void OnApplicationQuit() => Shutdown();

        /// <summary>
        /// Stops receiving log callbacks, flushes pending entries, and closes all writers.
        /// The operation is idempotent and safe to call from multiple Unity lifecycle paths.
        /// </summary>
        private void Shutdown()
        {
            lock (lifecycleLock)
            {
                if (isShuttingDown)
                {
                    return;
                }

                UnsubscribeFromUnityLogs();
                Flush();
                // Flush before marking the logger as shut down so pending application
                // messages are still mirrored to Unity's console. This is important for
                // editor tests using LogAssert.Expect as well as for normal final output.
                isShuttingDown = true;
                CloseWriters();
                Volatile.Write(ref initialized, 0);
            }
        }

        /// <summary>Queues an application message for console echoing and file output.</summary>
        /// <param name="level">The severity or category assigned to the message.</param>
        /// <param name="message">The message text to queue.</param>
        /// <exception cref="IOException">Thrown if lazy logger initialization cannot create the log path.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if lazy initialization cannot access the log path.</exception>
        private void Enqueue(LogLevel level, string message)
        {
            EnsureInitialized();
            if (isShuttingDown || Volatile.Read(ref initialized) == 0)
            {
                return;
            }

            logQueue.Enqueue(new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                EchoToUnity = true
            });
        }

        /// <summary>Queues a debug message when debug logging is enabled.</summary>
        /// <param name="message">The message text.</param>
        public static void Debug(string message)
        {
            try
            {
                if (Instance.enableDebugLogging) Instance.Enqueue(LogLevel.Debug, message);
            }
            catch (InvalidOperationException) { UnityEngine.Debug.Log("[DEBUG] " + message); }
        }

        /// <summary>Queues an informational message.</summary>
        /// <param name="message">The message text.</param>
        public static void Info(string message)
        {
            try { Instance.Enqueue(LogLevel.Info, message); }
            catch (InvalidOperationException) { UnityEngine.Debug.Log(message); }
        }

        /// <summary>Queues a warning message.</summary>
        /// <param name="message">The message text.</param>
        public static void Warning(string message)
        {
            try { Instance.Enqueue(LogLevel.Warning, message); }
            catch (InvalidOperationException) { UnityEngine.Debug.LogWarning(message); }
        }

        /// <summary>Queues an error message.</summary>
        /// <param name="message">The message text.</param>
        public static void Error(string message)
        {
            try { Instance.Enqueue(LogLevel.Error, message); }
            catch (InvalidOperationException) { UnityEngine.Debug.LogError(message); }
        }

        /// <summary>
        /// Drains the pending queue, echoes application messages, writes configured files,
        /// and notifies subscribers with the completed batch.
        /// </summary>
        private void Flush()
        {
            var newLogs = new List<LogEntry>();
            while (logQueue.TryDequeue(out LogEntry entry))
            {
                string line = $"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] {entry.Message}";

                if (entry.EchoToUnity)
                {
                    SafeUnityLog(entry.Level, entry.Message);
                }

                int mask = entry.Level switch
                {
                    LogLevel.Debug => debugTargets,
                    LogLevel.Info => infoTargets,
                    LogLevel.Warning => warningTargets,
                    LogLevel.Error => errorTargets,
                    _ => 0
                };

                for (int bit = 0; bit < 5; bit++)
                {
                    if ((mask & (1 << bit)) != 0 && writers.TryGetValue(bit, out StreamWriter writer))
                    {
                        try { writer.WriteLine(line); }
                        catch (ObjectDisposedException) { }
                        catch (IOException) { }
                    }
                }

                entry.Message = line;
                newLogs.Add(entry);
            }

            if (newLogs.Count == 0)
            {
                return;
            }

            try { OnNewLogBatch?.Invoke(newLogs); }
            catch { /* A subscriber must not break the logger's Update loop. */ }
        }

        /// <summary>
        /// Safely mirrors one application message to the Unity console.
        /// </summary>
        /// <remarks>
        /// Unity console output triggers Unity's log callback. The atomic
        /// <c>handlingUnityLog</c> flag marks this synchronous emission as internal,
        /// so <see cref="UnityLogHook"/> ignores the immediate callback. If Unity
        /// delivers a delayed threaded callback after the flag is cleared, the hook
        /// marks that queued entry as already emitted and it is not echoed again.
        /// The callback subscription is intentionally left untouched, avoiding the
        /// previous unsubscribe/re-subscribe race while notifications are in flight.
        /// </remarks>
        /// <param name="level">The logger level used to select Unity's console method.</param>
        /// <param name="message">The message text to send to the Unity console.</param>
        private void SafeUnityLog(LogLevel level, string message)
        {
            if (Volatile.Read(ref handlingUnityLog) != 0 || isShuttingDown || !logToUnityConsole)
            {
                return;
            }

            try
            {
                Interlocked.Exchange(ref handlingUnityLog, 1);
                switch (level)
                {
                    case LogLevel.Debug: UnityEngine.Debug.Log("[DEBUG] " + message); break;
                    case LogLevel.Warning: UnityEngine.Debug.LogWarning(message); break;
                    case LogLevel.Error: UnityEngine.Debug.LogError(message); break;
                    default: UnityEngine.Debug.Log(message); break;
                }
            }
            catch { /* Unity may already be shutting down. */ }
            finally
            {
                Volatile.Write(ref handlingUnityLog, 0);
            }
        }

#if UNITY_EDITOR
        /// <summary>Flushes pending entries and shuts the logger down for editor tests.</summary>
        public void FlushAndShutdown() => Shutdown();
#endif

        /// <summary>
        /// Deletes the oldest complete log-session groups until the configured retention
        /// count is met. A negative retention count keeps all files.
        /// </summary>
        /// <exception cref="IOException">A file deletion may fail if a file is locked; such failures are logged and ignored.</exception>
        /// <exception cref="UnauthorizedAccessException">A protected file may not be deleted; the failure is logged and ignored.</exception>
        public void CleanupOldLogFiles()
        {
            if (maxLogFileCount < 0 || !Directory.Exists(LogDirectory))
            {
                return;
            }

            var groups = new DirectoryInfo(LogDirectory)
                .GetFiles($"*{fileExtension}")
                .Select(file => new { File = file, Timestamp = ExtractTimestampFromFilename(file.Name) })
                .Where(item => !string.IsNullOrEmpty(item.Timestamp))
                .GroupBy(item => item.Timestamp)
                .OrderBy(group => group.Key)
                .ToList();

            int excessGroups = Math.Max(0, groups.Count - maxLogFileCount);
            for (int i = 0; i < excessGroups; i++)
            {
                foreach (var item in groups[i])
                {
                    try { item.File.Delete(); }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Logger could not delete {item.File.Name}: {ex}");
                    }
                }
            }
        }

        /// <summary>Subscribes the logger to Unity's threaded log notification exactly once.</summary>
        private void SubscribeToUnityLogs()
        {
            if (!hookSubscribed && !isShuttingDown)
            {
                Application.logMessageReceivedThreaded += UnityLogHook;
                hookSubscribed = true;
            }
        }

        /// <summary>Removes the Unity log callback if it is currently subscribed.</summary>
        private void UnsubscribeFromUnityLogs()
        {
            if (hookSubscribed)
            {
                Application.logMessageReceivedThreaded -= UnityLogHook;
                hookSubscribed = false;
            }
        }

        /// <summary>Disposes every open log writer and clears the writer table.</summary>
        private void CloseWriters()
        {
            foreach (StreamWriter writer in writers.Values)
            {
                try { writer.Dispose(); }
                catch { }
            }
            writers.Clear();
        }

        /// <summary>Extracts the session timestamp prefix from a log filename.</summary>
        /// <param name="filename">The filename to inspect.</param>
        /// <returns>The timestamp prefix, or an empty string when the filename is not recognized.</returns>
        private string ExtractTimestampFromFilename(string filename)
        {
            int firstUnderscore = filename.IndexOf('_');
            if (firstUnderscore < 0) return "";

            int secondUnderscore = filename.IndexOf('_', firstUnderscore + 1);
            return secondUnderscore < 0 ? "" : filename.Substring(0, secondUnderscore);
        }
    }
}
