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
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace MyDLP.EndPoint.Core
{
    class SeapClient
    {
        static SeapClient seapClient = null;
        int port = Configuration.SeapPort;
        String server = Configuration.SeapServer;
        TcpClient client;
        NetworkStream stream;
        int timeout = 5000;
        int responseLength = 256;
        int tryCount = 0;
        const int tryLimit = 3;


        public static FileOperation.Action GetWriteDecisionByPath(String filePath, String tempFilePath)
        {
            /*
            Logger.GetInstance().Debug("GetWriteDecisionByPath filePath:" + filePath + " tempFilePath:" + tempFilePath);
            SeapClient sClient = SeapClient.GetInstance();
            String response;
            String[] splitResp;
            long id;

            response = sClient.sendMessage("BEGIN");

            if (response.Equals("ERR"))
            {
                return FileOperation.Action.ALLOW;
            }
            else
            {
                splitResp = response.Split(' ');
                if (splitResp[0].Equals("OK"))
                {
                    id = Int64.Parse(splitResp[1]);
                }
                else
                {
                    return FileOperation.Action.ALLOW;
                }
            }

            response = sClient.sendMessage("PUSHFILE " + id + " " + tempFilePath);

            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            response = sClient.sendMessage("END " + id);

            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            response = sClient.sendMessage("ACLQ " + id);
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            if (splitResp[1].Equals("block"))
            {
                return FileOperation.Action.BLOCK;
            }
            else if (splitResp[1].Equals("pass"))
            {
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion
            */
            return FileOperation.Action.ALLOW;
        }

        public static FileOperation.Action GetWriteDecisionByCache(String filePath, MemoryStream cache) 
        {
            /*
            Logger.GetInstance().Debug("GetWriteDecisionByCache path: " + filePath +" length:" + cache.Length);
            SeapClient sClient = SeapClient.GetInstance();
            String response;
            String[] splitResp;
            long id;

            response = sClient.sendMessage("BEGIN");

            if (response.Equals("ERR"))
            {
                return FileOperation.Action.ALLOW;
            }
            else
            {
                splitResp = response.Split(' ');
                if (splitResp[0].Equals("OK"))
                {
                    id = Int64.Parse(splitResp[1]);
                }
                else
                {
                    return FileOperation.Action.ALLOW;
                }
            }
            String cmd = "PUSH " + id + " " + cache.Length + "\r\n";

            response = sClient.sendMessage(cmd, cache);
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            response = sClient.sendMessage("END " + id);

            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            response = sClient.sendMessage("ACLQ " + id);
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            if (splitResp[1].Equals("block"))
            {
                return FileOperation.Action.BLOCK;
            }
            else if (splitResp[1].Equals("pass"))
            {
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion
             */
            return FileOperation.Action.ALLOW;
        }

        public static FileOperation.Action GetReadDecisionByPath(String filePath)
        {
            //Logger.GetInstance().Debug("GetReadDecisionByPath path: " + Engine.GetShortPath(filePath));
            Logger.GetInstance().Debug("GetReadDecisionByPath path: " + filePath);
            SeapClient sClient = SeapClient.GetInstance();
            String response;
            String[] splitResp;
            long id;

            response = sClient.sendMessage("BEGIN");

            if (response.Equals("ERR"))
            {
                return FileOperation.Action.ALLOW;
            }
            else
            {
                splitResp = response.Split(' ');
                if (splitResp[0].Equals("OK"))
                {
                    id = Int64.Parse(splitResp[1]);
                }
                else
                {
                    return FileOperation.Action.ALLOW;
                }
            }

            response = sClient.sendMessage("PUSHFILE " + id + " " + Engine.GetShortPath(filePath));
            //response = sClient.sendMessage("PUSHFILE " + id + " " + filePath);
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            //response = sClient.sendMessage("SETPROP " + id + " filename=" + Engine.GetShortPath(filePath));
            response = sClient.sendMessage("SETPROP " + id + " filename=" + Path.GetFileName(filePath));
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            response = sClient.sendMessage("SETPROP " + id + " direction=in");
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }


            response = sClient.sendMessage("END " + id);
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            response = sClient.sendMessage("ACLQ " + id);
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            if (splitResp[1].Equals("block"))
            {
                return FileOperation.Action.BLOCK;
            }
            else if (splitResp[1].Equals("pass"))
            {
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion
            return FileOperation.Action.ALLOW;
        }

        private SeapClient()
        {
            try
            {
                Logger.GetInstance().Info("Initialize seap client server: " + server + " port: " + port);
                client = new TcpClient(server, port);
                stream = client.GetStream();
                stream.ReadTimeout = timeout;
                stream.WriteTimeout = timeout;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void Reconnect() 
        {
            try
            {
                Logger.GetInstance().Info("Reconnect seap client server: " + server + " port: " + port);
                client = new TcpClient(server, port);                
                stream = client.GetStream();
                stream.ReadTimeout = timeout;
                stream.WriteTimeout = timeout;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static SeapClient GetInstance()
        {
            try
            {
                if (seapClient == null)
                {
                    seapClient = new SeapClient();
                }
                return seapClient;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public String sendMessage(String cmd, MemoryStream msg)
        {
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(cmd);
            Byte[] end = System.Text.Encoding.ASCII.GetBytes("\r\n");
            Logger.GetInstance().Debug("SeapClient send message: <" + cmd + ">");
            int readCount;
            try
            {
                Byte[] response = new Byte[responseLength];
                lock (seapClient)
                {
                    stream.Write(data, 0, data.Length);
                    stream.Write(msg.GetBuffer(), 0, (int) msg.Length);
                    stream.Write(end, 0, end.Length);
                    readCount = stream.Read(response, 0, responseLength);
                    tryCount = 0;
                }
                String respMessage = System.Text.Encoding.ASCII.GetString(response, 0, readCount);
                Logger.GetInstance().Debug("SeapClient read response:  <" + respMessage.Trim() + ">");             
                return respMessage.Trim();
            }
            catch (System.IO.IOException)
            {
                if (tryCount <= tryLimit)
                {
                    Logger.GetInstance().Debug("IO Exception try reconnect try count:" + tryCount);
                    tryCount++;
                    Reconnect();
                    return sendMessage(cmd, msg);
                }
                else 
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        public String sendMessage(String msg)
        {
            Reconnect();
            int readCount;          
            Logger.GetInstance().Debug("SeapClient send message: <" + msg + ">");
            msg = msg + "\r\n";
            try
            {
                Byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
                Byte[] response = new Byte[responseLength];
                lock (seapClient)
                {
                    stream.Write(data, 0, msg.Length);
                    readCount = stream.Read(response, 0, responseLength);
                    tryCount = 0;
                }
                String respMessage = System.Text.Encoding.ASCII.GetString(response, 0, readCount);
                Logger.GetInstance().Debug("SeapClient read response:  <" + respMessage.Trim() + ">");             
                return respMessage.Trim();
            }
            catch(System.IO.IOException)
            {
                if (tryCount <= tryLimit)
                {
                    Logger.GetInstance().Debug("IO Exception try reconnect");
                    tryCount++;
                    Reconnect();
                    return sendMessage(msg);
                }
                else 
                {
                    throw;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
