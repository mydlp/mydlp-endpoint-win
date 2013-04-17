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
using System.IO;
namespace MyDLP.EndPoint.Core
{
    public class WriteFileOperation : FileOperation
    {
        public const int miniFilterBufferSize = 65536;
        public const int writeCacheLimit = 1048576; //1MB
        String tempFilePath;
        MemoryStream cache;
        int size;
        bool cached;

        public WriteFileOperation(String path, DateTime date)
        {
            type = FileOperation.OperationType.WRITE;
            this.path = path;
            this.date = date;
            cache = new MemoryStream();
            cached = true;
            size = 0;
        }

        public override string ToString()
        {
            return "WRITE " + "path: " + path + " date: " + date + " temp: " + tempFilePath;
        }

        public void createTempFile()
        {
            tempFilePath = Path.GetTempFileName();
        }

        public FileOperation.Action appendContent(byte[] content)
        {
            Logger.GetInstance().Debug("appendContent " + path);
            if (cached && (size + content.Length > writeCacheLimit)) 
            {
                if (tempFilePath == null)
                {
                    createTempFile();
                }

                using (FileStream stream = new FileStream(tempFilePath, FileMode.Append))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(cache.GetBuffer(),0,(int)cache.Length);
                        writer.Close();
                    }
                }
                Logger.GetInstance().Debug("appendContent cached set false " + path);
                cached = false;
                cache.Close();
            }

            if (cached)
            {
                
                Logger.GetInstance().Debug("appendContent cached size:" + size + " content.length:" + content.Length);
                cache.Write(content, 0, content.Length);
                size += content.Length;
            }
            else
            {
                if (tempFilePath == null)
                {
                    createTempFile();
                }

                using (FileStream stream = new FileStream(tempFilePath, FileMode.Append))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(content);
                        writer.Close();
                    }
                }
            }
            
            if (content.Length < miniFilterBufferSize)
            {
                Logger.GetInstance().Debug("appendContent call finish content.Length:" + content.Length + " limit:" + miniFilterBufferSize);
                return FinishWrite();
            }

            return FileOperation.Action.ALLOW;
        }

        public FileOperation.Action FinishWrite()
        {
            FileOperationController.GetInstance().DeleteOperation(this);        
            return DecideAction();
        }

        public FileOperation.Action DecideAction()
        {
            if (USBController.IsUsbBlocked())
            {
                return Action.BLOCK;
            }
            if (cached)
            {
                try
                {
                    return SeapClient.GetWriteDecisionByCache(path, cache);
                }
                catch(Exception e)
                {
                    Logger.GetInstance().Error("Exception" + e);
                    return Action.ALLOW;
                }

            }
            else
            {
                try
                {
                    return SeapClient.GetWriteDecisionByPath(path, tempFilePath);
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Error("Exception" + e);
                    return Action.ALLOW;
                }
            }
        }
    }
}
