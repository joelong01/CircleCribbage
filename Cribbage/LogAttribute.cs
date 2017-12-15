using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;

namespace Cribbage
{
    public enum LogLevel {None = 0, Function=1, State=2, Play=4, Animation=8};

    [System.AttributeUsage(AttributeTargets.Method, AllowMultiple=true) ]
    public class LogAttribute : System.Attribute
    {
        private string _entry;

        public string Entry
        {
            get { return _entry; }
            set { _entry = value; }
        }

        public LogLevel LogLevel = LogLevel.None;


        public LogAttribute(string entry)
        {
            _entry = entry;
            WriteLogEntry(this.GetType());
        }

        public void WriteLogEntry(Type t)
        {
            LogAttribute logEntry = t.GetTypeInfo().GetCustomAttribute<LogAttribute>();
            if (logEntry != null)
            {
                Debug.WriteLine("{0}\t{1}\t{2}", DateTime.Now, logEntry.LogLevel, logEntry.Entry);

            }

            


        }
    }
}
