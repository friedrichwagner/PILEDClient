using System;
using System.IO;
using System.Xml;
//using System.Windows.Forms;
using System.Diagnostics;

namespace Lumitech.Helpers
{
    //Singleton: There can only be one
    //http://www.codeproject.com/Articles/42354/The-Art-of-Logging
    //http://www.dofactory.com/Patterns/PatternSingleton.aspx#csharp


    public sealed class Logger
    {
        private enum LogLevels { INFO, WARN, ERROR, FATAL, DEBUG };

        private static Logger _instance = null;
        private static readonly object singletonLock = new object();

        private string LogFilePath;
        private string LogFileName;
        
        //be aware! this is static
        private static int iLogLevel=6; // 0 = Nichts, 10 = Alles
        public int LogLevel
        {
            get { return iLogLevel; }
            set { iLogLevel = value; Info("new loglevel=" + iLogLevel.ToString()); }
        }

        public static Logger GetInstance(string logfilename, int loglevel)
        {
            if (_instance == null)
            {
                lock (singletonLock)
                {
                    if (_instance == null)
                    {
                        _instance = new Logger(logfilename, loglevel);
                    }                    
                }
            }

            return _instance;
        }

        public static Logger GetInstance(int loglevel)
        {
            string fname = System.Reflection.Assembly.GetEntryAssembly().Location.Replace(".exe", ".log");
            _instance = Logger.GetInstance(fname, loglevel);
            return _instance;
        }

        public static Logger GetInstance()
        {
            string fname = System.Reflection.Assembly.GetEntryAssembly().Location.Replace(".exe", ".log");
            _instance = Logger.GetInstance(fname, iLogLevel);
            return _instance;
        }

        private Logger(string logfilename, int loglevel)
        {
            Logger.iLogLevel = loglevel;

            this.LogFileName = logfilename;

            LogFilePath = AppDomain.CurrentDomain.BaseDirectory + logfilename;

            if (!File.Exists(LogFilePath))
            {
                System.IO.StreamWriter sw = File.CreateText(LogFilePath);
                sw.Close();
            }         
        }

        /*public void Log(string msg)
        {
            //d.h. es wir immer geloggt
            Log(msg, "", iLogLevel);
        }*/

        public void Info(string msg)
        {
            //"[INFO]" wird immer gelogt
            StackTrace stackTrace = new StackTrace();
            Log(msg, "[INFO]", stackTrace.GetFrame(1).GetMethod().Name, iLogLevel);
        }

        public void Debug(string msg)
        {
            //"[DEBUG]" wird nur geloggt, wenn Level=10
            StackTrace stackTrace = new StackTrace();
            Log(msg, "[DEBUG]",  stackTrace.GetFrame(1).GetMethod().Name, 9);
        }

        public void Warn(string msg)
        {
            StackTrace stackTrace = new StackTrace();
            Log(msg, "[WARN]", stackTrace.GetFrame(1).GetMethod().Name, 5);
        }

        public void Error(string msg)
        {
            //"[ERROR]" wird immer geloggt
            StackTrace stackTrace = new StackTrace();
            Log(msg, "[ERROR]", stackTrace.GetFrame(1).GetMethod().Name, 0);
        }

        public void Fatal(string msg)
        {
            //"[FATAL]" wird immer geloggt
            StackTrace stackTrace = new StackTrace();
            Log(msg, "[FATAL]", stackTrace.GetFrame(1).GetMethod().Name, 0);
        }

        /*public void Log(string msg, int level )
        {
            Log(msg, "", level);
        }*/

        public void Log(string msg, string type, string where, int level)
        {
            lock (singletonLock)
            {
                if (level <= iLogLevel)
                {
                    using (StreamWriter sw = File.AppendText(LogFilePath))
                    {
                        string logLine = System.String.Format("{0:G}\t{1}\t{2}\t{3} ", System.DateTime.Now, type, where, msg);
                        sw.WriteLine(logLine);
                        sw.Flush();
                        sw.Close();
                    }
                }
            }
        }
    }
}
