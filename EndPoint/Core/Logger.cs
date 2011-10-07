//    Copyright (C) 2011 Huseyin Ozgur Batur <ozgur@medra.com.tr>
//
//--------------------------------------------------------------------------
//    This file is part of MyDLP.
//
//    MyDLP is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    MyDLP is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with MyDLP.  If not, see <http://www.gnu.org/licenses/>.
//--------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics; 

namespace MyDLP.EndPoint.Core
{
    public class Logger
    {
        private static Logger logger;
        EventLog eventLogger; 
        String logPath;
        public enum LogLevel { ERROR = 0, INFO, DEBUG };
        LogLevel currentLevel;
        bool useWindowsLogger = false;
        bool useFileLogger = true;
        bool consoleLogger = false;


        private Logger()
        {
            logPath = Configuration.GetLogPath();
            Configuration.InitLogLevel();
            currentLevel = Configuration.LogLevel;
            useWindowsLogger = false;
            useFileLogger = true;
            consoleLogger = false;
        }

        public void InitializeWatchdogLogger(System.Diagnostics.EventLog eventLog)
        {
            logger.eventLogger = eventLog;
            logger.logPath = WatchdogConfiguration.GetLogPath();
        }

        public void InitializeMainLogger(System.Diagnostics.EventLog eventLog)
        {
            logger.eventLogger = eventLog;          
        }

        public static Logger GetInstance()
        {
            if (logger == null)
            {
                logger = new Logger();

                if (logger.currentLevel == LogLevel.DEBUG && Environment.UserInteractive == true)
                {
                    logger.consoleLogger = true;
                }
            }
            return logger;
        }

        public void Debug(String entry)
        {
            String logEntry = DateTime.Now + " DEBUG " + entry;
            if (currentLevel == LogLevel.DEBUG)
            {
                if (useFileLogger)
                {
                    lock (logPath)
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true))
                        {                            
                            file.WriteLine(logEntry);                          
                        }
                    }
                }

                if (consoleLogger)
                {
                    Console.WriteLine(logEntry);
                }
                //No debug log in windows event logger
            }
        }

        public void Info(String entry)
        {
            String logEntry = DateTime.Now + " INFO  " + entry;
            if (currentLevel == LogLevel.DEBUG || currentLevel == LogLevel.INFO)
            {
                lock (logPath)
                {
                    if (useFileLogger)
                    {
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true))
                        {                           
                            file.WriteLine(logEntry);
                        }
                    }
                }

                if (consoleLogger)
                {
                    Console.WriteLine(logEntry);
                }

                if (useWindowsLogger)
                {
                    //TODO:windows event log
                }
            }
        }

        public void Error(String entry)
        {          
            String logEntry = DateTime.Now + " ERROR " + entry;
            if (useFileLogger)
            {
                lock (logPath)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true))
                    {                        
                        file.WriteLine(logEntry);
                    }
                }

                if (consoleLogger)
                {
                    Console.WriteLine(logEntry);
                }
            }

            if (useWindowsLogger)
            {
                //TODO:windows event log
            }

        }
    }
}
