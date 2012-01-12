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
using System.Collections;
using System.Timers;


namespace MyDLP.EndPoint.Core
{
    public class FileOperationController
    {

        private static FileOperationController controller;

        Hashtable operationTable;

        Timer tableTimer;

        //default timer period is 1 minute
        int tableTimerPeriod = 10000;

        public FileOperation.Action HandleOpenOperation(string filePath)
        {
            OpenFileOperation oop = new OpenFileOperation(filePath, System.DateTime.UtcNow);
            if (!operationTable.Contains(filePath))
            {
                lock (operationTable)
                {
                    operationTable.Add(filePath, new FileOperationTableEntry(oop));
                }
                Logger.GetInstance().Debug("HandleOpenOperation added file: " + filePath);
                return oop.DecideAction();
            }
            else
            {
                return ((FileOperationTableEntry)operationTable[filePath]).Update(oop);

            }

        }

        public void HandleCleanupOperation(string filePath)
        {
            Logger.GetInstance().Debug("HandleCleanupWriteOperation path:" + filePath);
            if (operationTable.Contains(filePath) && ((FileOperationTableEntry)operationTable[filePath]).write != null)
            {
               ((FileOperationTableEntry)operationTable[filePath]).write.FinishWrite();
            }
        }

        public FileOperation.Action HandleWriteOperation(string filePath, byte[] content, int length)
        {
            Logger.GetInstance().Debug("HandleCleanupWriteOperation path:" + filePath + "length: " + length);
            WriteFileOperation wop;

            if (!operationTable.Contains(filePath))
            {
                wop = new WriteFileOperation(filePath, System.DateTime.UtcNow);
                lock (operationTable)
                {
                    operationTable.Add(filePath, new FileOperationTableEntry(wop));
                }
            }
            else
            {
                if (((FileOperationTableEntry)operationTable[filePath]).write == null)
                {
                    wop = new WriteFileOperation(filePath, System.DateTime.UtcNow);
                }
                else
                {
                    wop = ((FileOperationTableEntry)operationTable[filePath]).write;
                }

                ((FileOperationTableEntry)operationTable[filePath]).Update(wop);
            }
            FileOperation.Action action = wop.appendContent(content);
            return action;
        }

        public void DeleteOperation(WriteFileOperation wop)
        {
            if (operationTable.Contains(wop.path))
            {

                FileOperationTableEntry entry = (FileOperationTableEntry)operationTable[wop.path];
                entry.write = null;
                if (entry.open == null)
                {
                    lock (operationTable)
                    {
                        operationTable.Remove(wop.path);
                    }
                }
            }
        }

        public void DeleteOperation(OpenFileOperation oop)
        {
            if (operationTable.Contains(oop.path))
            {

                FileOperationTableEntry entry = (FileOperationTableEntry)operationTable[oop.path];
                entry.open = null;
                if (entry.write == null)
                {
                    lock (operationTable)
                    {
                        operationTable.Remove(oop.path);
                    }
                }
            }
        }

        private void OnTimedTableEvent(object source, ElapsedEventArgs e)
        {
            ArrayList deletedOps = new ArrayList();
            lock (operationTable)
            {
                foreach (FileOperationTableEntry en in operationTable.Values)
                {
                    if (en.open != null && (System.DateTime.UtcNow - en.open.date > new TimeSpan(0, 0, OpenFileOperation.suppressOpenInterval)))
                    {
                        deletedOps.Add(en.open);
                    }
                    else
                    {
                    //    Logger.GetInstance().Debug("OnTimedTableEvent deleted " + en);
                    }
                }
            }
            foreach (OpenFileOperation op in deletedOps)
            {
                controller.DeleteOperation(op);
            }
        }

        public static FileOperationController GetInstance()
        {
            if (controller == null)
            {
                controller = new FileOperationController();
            }
            return controller;
        }

        private FileOperationController()
        {
            operationTable = new Hashtable();
            tableTimer = new Timer(tableTimerPeriod);
            tableTimer.Elapsed += new ElapsedEventHandler(OnTimedTableEvent);
            tableTimer.Enabled = true;
        }
    }
}
