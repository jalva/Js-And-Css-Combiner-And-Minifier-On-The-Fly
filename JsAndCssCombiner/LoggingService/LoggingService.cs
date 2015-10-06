using System.Diagnostics;
using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace JsAndCssCombiner.LoggingService
{
    // implementation taken from: http://www.martinwilley.com/net/code/entliblogging.html#ToC1
    public class LoggingService : ILoggingService
    {
        /*
         * We don't use priority or eventId
         * We get the calling method from the stackFrame (careful with inlining here)
         */
        private static string GetCallingMethod(StackFrame frame)
        {
            //don't calculate this if we aren't logging
            if (!Logger.IsLoggingEnabled()) return string.Empty;
            var method = frame.GetMethod();
            return method.DeclaringType.FullName + "." + method.Name;
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(string message)
        {
            var category = GetCallingMethod(new StackFrame(1)); 
            Logger.Write(message, category, 0, 0, TraceEventType.Information);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warn(string message)
        {
            var category = GetCallingMethod(new StackFrame(1));
            Logger.Write(message, category, 0, 0, TraceEventType.Warning);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(string message)
        {
            var category = GetCallingMethod(new StackFrame(1));
            Logger.Write(message, category, 0, 0, TraceEventType.Error);
        }
    }
}
