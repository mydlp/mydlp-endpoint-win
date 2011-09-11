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

namespace MyDLP.EndPoint.Core
{
    public class Logger
    {
        private static Logger logger;
        String logPath = @"c:\mydlpepwin.log";
        public enum LogLevel { ERROR, INFO, DEBUG };
        LogLevel currentLevel = LogLevel.DEBUG;
        bool useWindowsLogger = false;
        bool useFileLogger = true;

        public static Logger GetInstance()
        {
            if (logger == null)
            {
                logger = new Logger();
            }
            return logger;
        }

        public void Debug(String entry)
        {
            if (currentLevel == LogLevel.DEBUG)
            {
                if (useFileLogger)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true))
                    {
                        file.WriteLine("DEBUG " + entry + " " + DateTime.UtcNow);
                    }
                }
                //No debug log in windows event logger
            }
        }

        public void Info(String entry)
        {
            if (currentLevel == LogLevel.DEBUG || currentLevel == LogLevel.INFO)
            {
                if (useFileLogger)
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true))
                    {
                        file.WriteLine("INFO " + entry + " " + DateTime.UtcNow);
                    }
                }

                if (useWindowsLogger)
                {
                    //TODO:windows event log
                }
            }
        }

        public void Error(String entry)
        {
            if (useFileLogger)
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(logPath, true))
                {
                    file.WriteLine("ERROR " + entry + " " + DateTime.UtcNow);
                }
            }

            if (useWindowsLogger)
            {
                //TODO:windows event log
            }

        }
    }
}
