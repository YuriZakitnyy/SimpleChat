using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

namespace ChatClientCommon
{
    public enum LoggerSeverity
    {
        None,
        Debug,
        Messg,
        Error,
        Silent,
        TurnedOff = int.MaxValue
    }

    public class Logger
    {
        private static Logger instance = new Logger();
        public static Logger Instance => instance;

        private string logPath;
        private string logFile;
        private List<string> silentLogs;

        public string LogFile { get { return logFile; } }
        public string LogPath { get { return logPath; } }
        public static string EventLogSource { get; set; } = CommonConstants.AppName;
        public static string EventLogVersion { get; set; } = "1";

        private Logger()
        {
            logPath = CommonConstants.OutputDirectory;
            logFile = Path.Combine(logPath, "log.txt");
            silentLogs = new List<string>();

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        public void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledError(sender, e.ExceptionObject, "CurrentDomain_UnhandledException");
        }

        #region Static methods

        public static void EventLogError(string message)
        {
            try
            {
                EventLog.WriteEntry(EventLogSource, message, EventLogEntryType.Error);
            }
            catch
            {
            }
        }

        public static void Debug(object sender, string format, params object[] args)
        {
            instance.DoLog(sender, LoggerSeverity.Debug, false, format, args);
        }

        public static void Messg(object sender, string format, params object[] args)
        {
            instance.DoLog(sender, LoggerSeverity.Messg, false, format, args);
        }

        public static void Error(object sender, Exception ex)
        {
            instance.DoLog(sender, LoggerSeverity.Error, false, "Exception: {0} {1}", ex.Message, ex.ToString());
        }

        public static void UnhandledError(object sender, object ex, string subject)
        {
            instance.DoLog(sender, LoggerSeverity.Error, true, "Unhandled exception: {0} {1}", ex, subject);
        }

        #endregion Static methods

        #region log reading/writing

        private void DoLog(object sender, LoggerSeverity severity, bool crash, string format, params object[] args)
        {
            try
            {
                string date = DateTime.UtcNow.ToString(CommonConstants.LogDateFormat);
                string severityText = severity.ToString();
                severityText = severityText.ToUpperInvariant();
                string source = sender == null ? string.Format("{0} - {1}", EventLogSource, EventLogVersion) : string.Format("{0}/{1} - {2}", EventLogSource, sender.GetType().ToString(), EventLogVersion);
                string msg = string.Format(format, args);
                if (crash)
                {
                    EventLogError(msg);
                    AppendToFile(msg);
                }
                string xml = MakeXml(date, severity, source, 0, msg, args);
                System.Diagnostics.Debug.WriteLine(xml);
                AppendLogs(xml, AppendToFile);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        private void AppendLogs(string xml, Action<string> appender)
        {
            try
            {
                lock (silentLogs)
                {
                    appender(xml);
                }
            }
            catch
            {
            }
        }

        private static string MakeXml(string date, LoggerSeverity severity, string source, int errorCode, string msg, params object[] args)
        {
            string result = string.Empty;
            XmlDocument doc = new XmlDocument();

            var entry = doc.CreateElement("entry");
            doc.AppendChild(entry);

            entry.Attributes.Append(doc.CreateAttribute("severity")).Value = severity.ToString().ToUpperInvariant();
            entry.Attributes.Append(doc.CreateAttribute("source")).Value = source;
            entry.Attributes.Append(doc.CreateAttribute("date")).Value = date;
            entry.Attributes.Append(doc.CreateAttribute("code")).Value = errorCode.ToString();

            if (errorCode == 0)
                entry.InnerText = msg;
            else
            {
                foreach (var obj in args)
                {
                    var paramEntry = entry.AppendChild(doc.CreateElement("param"));
                    paramEntry.InnerText = obj == null ? "null" : obj.ToString();
                }
            }

            result = doc.InnerXml;
            return result;
        }

        private void AppendToFile(string xml)
        {
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }

            StreamWriter file;

            if (!File.Exists(logFile))
            {
                file = File.CreateText(logFile);
            }
            else
            {
                file = File.AppendText(logFile);
            }

            try
            {
                file.WriteLine(xml);
            }
            catch
            {

            }
            finally
            {
                file.Dispose();
            }
        }

        public static void RemoveLogs()
        {
            MoveFileEx(Instance.logFile, null, MoveFileFlags.DelayUntilReboot);
        }

        [Flags]
        public enum MoveFileFlags
        {
            None = 0,
            // Other flags...
            DelayUntilReboot = 4, // Key flag for scheduling deletion
                                  // Other flags...
        }

        /// <summary>
        /// Imports the MoveFileEx function from kernel32.dll.
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool MoveFileEx(
            string lpExistingFileName,
            string lpNewFileName, // Set to null for deletion
            MoveFileFlags dwFlags);

        #endregion
    }
}
