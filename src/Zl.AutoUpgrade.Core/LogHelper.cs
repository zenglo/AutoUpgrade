using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace Zl.AutoUpgrade.Core
{
    class LogHelper
    {
        public static void Log(Exception e)
        {
            Log(e, null);
        }

        public static void Log(string info)
        {
            Log(info, (string)null);
        }
        public static void Log(string info, string rem, bool closeTime)
        {
            Log(info, null, rem, closeTime);
        }
        public static void Log(string info, Exception e)
        {
            Log(info, e, null);
        }

        public static void Log(Exception e, string rem)
        {
            Log(null, e, rem);
        }

        public static void Log(string info, string rem)
        {
            Log(info, null, rem);
        }

        public static void Log(string info, Exception e, string rem)
        {
            Log(info, e, rem, false);
        }
        public static void Log(string info, Exception e, string rem, bool closetime)
        {
            try
            {
                string logfile = AppDomain.CurrentDomain.BaseDirectory + "\\log\\" +
                    (e == null ? "info_" : "exception_") +
                    DateTime.Now.ToString("yyyy-MM-dd") +
                    (rem == null ? "" : ("_" + rem)) + ".log";
                CreateLogDirectory();
                FileStream file = null;
                using (file = new FileStream(logfile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                {
                    StreamWriter sw = new StreamWriter(file, Encoding.UTF8);
                    if (!closetime)
                        sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    if (info != null)
                        sw.WriteLine(info);
                    if (e != null)
                        sw.WriteLine(e.ToString());
                    sw.Flush();
                    sw.Close();
                }
            }
            catch (Exception)
            {
            }
        }

        public static void CreateLogDirectory()
        {
            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "\\log");
        }
    }
}
