using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using MLGWorks.Utils.Patterns;

namespace MLGWorks.Utils.Logging
{
    public enum LogLocationType
    {
        PersistentDataPath,
        DataPath,
        Custom
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    [DefaultExecutionOrder(-1000)]
    public class Logger : Singleton<Logger>
    {
        [Header("Log File Location")]
        public LogLocationType pathType = LogLocationType.PersistentDataPath;
        public string relativePath = "Logs";
        public string customPath = "";

        [Header("Logging Options")]
        public bool enableDebugLogging = true;

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

        public string LogDirectory
        {
            get
            {
                string basePath = pathType switch
                {
                    LogLocationType.DataPath => Application.dataPath,
                    LogLocationType.Custom => customPath,
                    _ => Application.persistentDataPath
                };
                return Path.Combine(basePath, relativePath);
            }
        }

        private struct LogEntry
        {
            public DateTime Timestamp;
            public LogLevel Level;
            public string Message;
        }

        private readonly ConcurrentQueue<LogEntry> logQueue = new();
        private readonly Dictionary<int, StreamWriter> writers = new();
        private bool initialized;
        private bool isHandlingUnityLog = false;

        protected override void Awake()
        {
            base.Awake();
            // Lazy initialization will occur when logging happens
        }

        private void EnsureInitialized()
        {
            if (!initialized)
                Initialize();
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;

            Directory.CreateDirectory(LogDirectory);
            string ts = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            var needed = new HashSet<int>();
            void Mark(int mask)
            {
                for (int bit = 0; bit < 5; bit++)
                    if ((mask & (1 << bit)) != 0)
                        needed.Add(bit);
            }

            Mark(debugTargets);
            Mark(infoTargets);
            Mark(warningTargets);
            Mark(errorTargets);

            foreach (int i in needed)
            {
                string name = i switch
                {
                    0 => "debug",
                    1 => "info",
                    2 => "warning",
                    3 => "error",
                    4 => "combined",
                    _ => "unknown"
                };
                string filename = $"{ts}_{name}{fileExtension}";
                var path = Path.Combine(LogDirectory, filename);
                try
                {
                    writers[i] = new StreamWriter(path, append: true) { AutoFlush = true };
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Logger failed to create '{filename}': {ex}");
                }
            }

            Application.logMessageReceivedThreaded += UnityLogHook;
            CleanupOldLogFiles();
        }

        public void EmitTestLogs()
        {
            Logger.Debug("This is a test Debug log.");
            Logger.Info("This is a test Info log.");
            Logger.Warning("This is a test Warning log.");
            Logger.Error("This is a test Error log.");
        }

        private void UnityLogHook(string condition, string stackTrace, LogType type)
        {
            if (isHandlingUnityLog) return;

            try
            {
                isHandlingUnityLog = true;

                EnsureInitialized();

                switch (type)
                {
                    case LogType.Log:
                        Info("[UNITY] " + condition);
                        break;

                    case LogType.Warning:
                        Warning("[UNITY] " + condition);
                        break;

                    case LogType.Error:
                    case LogType.Exception:
                        Error("[UNITY] " + condition + "\n" + stackTrace);
                        break;
                }
            }
            finally
            {
                isHandlingUnityLog = false;
            }
        }

        private void Update() => Flush();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Shutdown();
        }

        private void OnApplicationQuit() => Shutdown();

        private void Shutdown()
        {
            Flush();
            foreach (var w in writers.Values)
                w.Close();
        }

        private void Enqueue(LogLevel level, string message)
        {
            EnsureInitialized();
            logQueue.Enqueue(new LogEntry { Timestamp = DateTime.Now, Level = level, Message = message });
        }

        public static void Debug(string msg)
        {
            if (Instance.enableDebugLogging)
                Instance.Enqueue(LogLevel.Debug, msg);
        }

        public static void Info(string msg) => Instance.Enqueue(LogLevel.Info, msg);

        public static void Warning(string msg) => Instance.Enqueue(LogLevel.Warning, msg);

        public static void Error(string msg) => Instance.Enqueue(LogLevel.Error, msg);

        private void Flush()
        {
            while (logQueue.TryDequeue(out var e))
            {
                string line = $"[{e.Timestamp:HH:mm:ss.fff}] [{e.Level}] {e.Message}";

                // Use loop-safe Unity logging
                SafeUnityLog(e.Level, e.Message);

                int mask = e.Level switch
                {
                    LogLevel.Debug => debugTargets,
                    LogLevel.Info => infoTargets,
                    LogLevel.Warning => warningTargets,
                    LogLevel.Error => errorTargets,
                    _ => 0
                };

                for (int bit = 0; bit < 5; bit++)
                {
                    if ((mask & (1 << bit)) != 0 && writers.TryGetValue(bit, out var w))
                        w.WriteLine(line);
                }
            }
        }

        private void SafeUnityLog(LogLevel level, string message)
        {
            if (isHandlingUnityLog) return;

            try
            {
                isHandlingUnityLog = true;

                switch (level)
                {
                    case LogLevel.Debug:
                        UnityEngine.Debug.Log("[DEBUG] " + message);
                        break;

                    case LogLevel.Info:
                        UnityEngine.Debug.Log(message);
                        break;

                    case LogLevel.Warning:
                        UnityEngine.Debug.LogWarning(message);
                        break;

                    case LogLevel.Error:
                        UnityEngine.Debug.LogError(message);
                        break;
                }
            }
            finally
            {
                isHandlingUnityLog = false;
            }
        }

#if UNITY_EDITOR

        public void FlushAndShutdown()
        {
            Flush();
            foreach (var writer in writers.Values)
                writer.Close();
            writers.Clear();
        }

#endif

        public void CleanupOldLogFiles()
        {
            Application.logMessageReceivedThreaded -= UnityLogHook;

            if (maxLogFileCount < 0) return;
            if (!Directory.Exists(LogDirectory)) return;

            var files = new DirectoryInfo(LogDirectory)
                .GetFiles($"*{fileExtension}");

            // Extract timestamps from filenames and group files by timestamp string
            var groups = files
                .GroupBy(f => ExtractTimestampFromFilename(f.Name))
                .OrderBy(g => g.Key)  // Oldest timestamp first
                .ToList();

            int excessGroups = groups.Count - maxLogFileCount;
            for (int i = 0; i < excessGroups; i++)
            {
                foreach (var file in groups[i])
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Logger could not delete {file.Name}: {ex}");
                    }
                }
            }

            Application.logMessageReceivedThreaded += UnityLogHook;
        }

        // Helper: Extract timestamp substring from filename
        private string ExtractTimestampFromFilename(string filename)
        {
            // Filename example: "2025-07-20_12-30-00_log_debug.log"
            // Extract timestamp at the start, until the first underscore after date/time

            int firstUnderscore = filename.IndexOf('_');
            if (firstUnderscore < 0)
                return "";

            // The timestamp is everything from start up to the second underscore (date + time)
            // Example: "2025-07-20_12-30-00"
            // So find second underscore, which separates time and rest of filename

            int secondUnderscore = filename.IndexOf('_', firstUnderscore + 1);
            if (secondUnderscore < 0)
                return "";

            // Extract substring from start to second underscore (inclusive of time part)
            return filename.Substring(0, secondUnderscore);
        }
    }
}
