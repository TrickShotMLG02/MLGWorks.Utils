using MLGWorks.Utils.Logging;
using NUnit.Framework;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.TestTools;
using Logger = MLGWorks.Utils.Logging.Logger;

namespace MLGWorks.Utils.Tests.Logging
{
    public class LoggerTests
    {
        private Logger logger;
        private string logDir;
        private string fileExt;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Create a new GameObject and add Logger component
            var go = new GameObject("LoggerTestObject");
            logger = go.AddComponent<Logger>();

            // set custom path for testing only
            string baseLogDir = logger.LogDirectory; // this is the default or configured log directory
            string customLogDir = Path.Combine(baseLogDir, "TestRuns");

            logger.pathType = LogLocationType.Custom;
            logger.customPath = customLogDir;
            logger.relativePath = "";

            logDir = logger.LogDirectory;
            fileExt = logger.fileExtension;

            // Clean up any leftover logs before tests
            if (Directory.Exists(logDir))
                Directory.Delete(logDir, true);
            Directory.CreateDirectory(logDir);
        }

        [SetUp]
        public void Setup()
        {
            // Reset pruning to default before each test
            logger.maxLogFileCount = 10;
        }

        [TearDown]
        public void CleanupLogs()
        {
            var logger = UnityEngine.Object.FindObjectOfType<Logger>();
            if (logger == null) return;

            string logDir = logger.LogDirectory;

            if (Directory.Exists(logDir))
            {
                var files = Directory.GetFiles(logDir, $"*{logger.fileExtension}");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning($"Failed to delete log file {file}: {ex.Message}");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator LogAllLevels_WritesToExpectedFiles()
        {
            LogAssert.Expect(LogType.Log, "[DEBUG] Debug message");
            LogAssert.Expect(LogType.Log, "Info message");
            LogAssert.Expect(LogType.Warning, "Warning message");
            LogAssert.Expect(LogType.Error, "Error message");

            // Write one log per level
            Logger.Debug("Debug message");
            Logger.Info("Info message");
            Logger.Warning("Warning message");
            Logger.Error("Error message");

            // Wait one frame to flush logs
            yield return null;

            // Flush synchronously for editor tests
#if UNITY_EDITOR
            logger.FlushAndShutdown();
#endif

            // Expect one session's files created (by timestamp prefix)
            var files = Directory.GetFiles(logDir, $"*{fileExt}").Select(Path.GetFileName).ToList();
            Assert.IsNotEmpty(files, "No log files found after logging");

            // Check expected log level file presence for this session
            bool debugFileFound = files.Any(f => f.Contains("_debug"));
            bool infoFileFound = files.Any(f => f.Contains("_info"));
            bool warningFileFound = files.Any(f => f.Contains("_warning"));
            bool errorFileFound = files.Any(f => f.Contains("_error"));
            bool combinedFileFound = files.Any(f => f.Contains("_combined"));

            Assert.IsTrue(debugFileFound, "Debug log file missing");
            Assert.IsTrue(infoFileFound, "Info log file missing");
            Assert.IsTrue(warningFileFound, "Warning log file missing");
            Assert.IsTrue(errorFileFound, "Error log file missing");
            Assert.IsTrue(combinedFileFound, "Combined log file missing");
        }

        [UnityTest]
        public IEnumerator CleanupOldLogFiles_PrunesByTimestampGroups()
        {
            logger.maxLogFileCount = 2; // prune to 2 sessions max

            // Create 4 log sessions with all 5 log level files each
            string[] levels = { "debug", "info", "warning", "error", "combined" };
            Directory.CreateDirectory(logDir);

            for (int session = 0; session < 4; session++)
            {
                string timestamp = DateTime.Now.AddSeconds(session).ToString("yyyy-MM-dd_HH-mm-ss");
                foreach (var level in levels)
                {
                    string filename = $"{timestamp}_{level}{fileExt}";
                    File.WriteAllText(Path.Combine(logDir, filename), "dummy log");
                }
                System.Threading.Thread.Sleep(50); // slight delay for unique timestamp
            }

            // Run prune
            logger.CleanupOldLogFiles();

            yield return null;

#if UNITY_EDITOR
            logger.FlushAndShutdown();
#endif

            // Get remaining files and group by timestamp prefix
            var remainingFiles = Directory.GetFiles(logDir, $"*{fileExt}").Select(Path.GetFileName).ToList();
            var remainingGroups = remainingFiles
                .GroupBy(f => ExtractTimestampFromFilename(f))
                .ToList();

            Assert.LessOrEqual(remainingGroups.Count, logger.maxLogFileCount,
                $"Expected at most {logger.maxLogFileCount} log sessions after pruning, found {remainingGroups.Count}");
        }

        [Test]
        public void ExtractTimestampFromFilename_WorksCorrectly()
        {
            string filename = "2025-07-20_12-30-00_debug.log";
            string ts = ExtractTimestampFromFilename(filename);
            Assert.AreEqual("2025-07-20_12-30-00", ts);

            filename = "2025-07-20_12-30-00_combined.log";
            ts = ExtractTimestampFromFilename(filename);
            Assert.AreEqual("2025-07-20_12-30-00", ts);

            filename = "no_underscore.log";
            ts = ExtractTimestampFromFilename(filename);
            Assert.AreEqual("", ts);
        }

        [UnityTest]
        public IEnumerator Logger_DoesNotInfiniteLoopOnUnityLogs()
        {
            // This test triggers Unity logs and verifies no infinite recursion occurs
            int maxLogs = 5;
            int logCount = 0;

            Application.logMessageReceivedThreaded += (condition, stackTrace, type) =>
            {
                logCount++;
            };

            for (int i = 0; i < maxLogs; i++)
            {
                Logger.Info($"Test message {i}");
            }

            // Wait a few frames to allow logging and flushing
            for (int i = 0; i < 5; i++)
                yield return null;

            Assert.LessOrEqual(logCount, maxLogs * 2, "Possible infinite log recursion detected");
        }

        private string ExtractTimestampFromFilename(string filename)
        {
            int firstUnderscore = filename.IndexOf('_');
            if (firstUnderscore < 0)
                return "";

            int secondUnderscore = filename.IndexOf('_', firstUnderscore + 1);
            if (secondUnderscore < 0)
                return "";

            return filename.Substring(0, secondUnderscore);
        }
    }
}
