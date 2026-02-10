using System;

namespace ChatClientCommon
{
    public class CommonConstants
    {
        public const string AppName = "ChatClient";
        public const string AppDirectory = "ChatClient.log";
        public const string LogDateFormat = "yyyyMMdd HH:mm:ss";
        public const int MaxMessageBytes = 5 * 1024 * 1024;
        public static string OutputDirectory => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppName);
    }
}
