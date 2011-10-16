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
        int timeout = 50000;
        int responseLength = 512;
        int tryCount = 0;
        const int tryLimit = 3;


        public static FileOperation.Action GetWriteDecisionByPath(String filePath, String tempFilePath)
        {
            
            //Logger.GetInstance().Debug("GetWriteDecisionByPath filePath:" + filePath + " tempFilePath:" + tempFilePath);
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


            //response = sClient.sendMessage("SETPROP " + id + " filename=" + Engine.GetShortPath(filePath));
            response = sClient.sendMessage("SETPROP " + id + " filename=" + Path.GetFileName(filePath));
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            response = sClient.sendMessage("SETPROP " + id + " burn_after_reading=true");
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
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
            
            sClient.sendMessage("DESTROY " + id);           

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

        public static FileOperation.Action GetWriteDecisionByCache(String filePath, MemoryStream cache) 
        {
            
            //Logger.GetInstance().Debug("GetWriteDecisionByCache path: " + filePath +" length:" + cache.Length);
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

            response = sClient.sendMessage("SETPROP " + id + " filename=" + Path.GetFileName(filePath));
            splitResp = response.Split(' ');
            if (!splitResp[0].Equals("OK"))
            {
                return FileOperation.Action.ALLOW;
            }

            String cmd = "PUSH " + id + " " + cache.Length;

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
            
            sClient.sendMessage("DESTROY " + id);           

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

        public static FileOperation.Action GetReadDecisionByPath(String filePath)
        {
            SeapClient sClient = SeapClient.GetInstance();
            sClient.tryCount = 0;  
            try
            {
                //Logger.GetInstance().Debug("GetReadDecisionByPath path: " + Engine.GetShortPath(filePath));
                Logger.GetInstance().Debug("GetReadDecisionByPath path: " + filePath);
               
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

                sClient.sendMessage("DESTROY " + id);

                if (splitResp[1].Equals("block"))
                {
                    return FileOperation.Action.BLOCK;
                }
                else if (splitResp[1].Equals("pass"))
                {
                    return FileOperation.Action.ALLOW;
                }
            }
            catch 
            {
                //todo: Default Acion
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
                try
                {
                    client.Close();
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Info("Reconnect unable to close client: " + e.Message + " " + e.StackTrace);
                }

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
            Reconnect();
            Byte[] cmdBin = System.Text.Encoding.ASCII.GetBytes(cmd);
            Byte[] end = System.Text.Encoding.ASCII.GetBytes("\r\n");
            Logger.GetInstance().Debug("SeapClient send message: <" + cmd + ">");
            Byte[] response = new Byte[responseLength];
            int readCount;
            try
            {                
                lock (seapClient)
                {
                    stream.Write(cmdBin, 0, cmdBin.Length);
                    stream.Write(end, 0, end.Length);
                    stream.Write(msg.GetBuffer(), 0, (int) msg.Length);
                    //Logger.GetInstance().Debug("SeapClient send data: <" + System.Text.Encoding.ASCII.GetString(msg.GetBuffer()) + ">");                     
                    //this not necessary
                    //stream.Write(end, 0, end.Length);
                    stream.Flush();
                    readCount = stream.Read(response, 0, responseLength);
                    tryCount = 0;
                }
                String respMessage = System.Text.Encoding.ASCII.GetString(response, 0, readCount);
                Logger.GetInstance().Debug("SeapClient read response:  <" + respMessage.Trim() + ">");             
                return respMessage.Trim();
            }
            catch (System.IO.IOException e)
            {
                /*
                if (tryCount < tryLimit)
                {
                    Logger.GetInstance().Debug("IO Exception try reconnect try count:" + tryCount);
                    tryCount++;
                    //consume &discard error message
                    try
                    {
                        readCount = stream.Read(response, 0, responseLength);
                    }
                    catch
                    {
                        Logger.GetInstance().Debug("SeapClient discard not possible");  
                    }
                    try
                    {
                        Reconnect();
                    }
                    catch
                    {
                        Logger.GetInstance().Debug("REconnect failed");
                    }
                    return sendMessage(cmd.TrimEnd(), msg);
                }
                else 
                {
                    Logger.GetInstance().Debug("IO Exception exceded try reconnect try count:" + tryCount);
                    throw;
                }*/
                throw;
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
            Byte[] response = new Byte[responseLength];
            msg = msg + "\r\n";
            try
            {
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);
                lock (seapClient)
                {
                    if (stream.DataAvailable == true)
                        readCount = stream.Read(response, 0, responseLength);
                    stream.Write(data, 0, msg.Length);
                    stream.Flush();
                    readCount = stream.Read(response, 0, responseLength);
                    tryCount = 0;
                }
                String respMessage = System.Text.Encoding.ASCII.GetString(response, 0, readCount);
                Logger.GetInstance().Debug("SeapClient read response:  <" + respMessage.Trim() + ">");             
                return respMessage.Trim();
            }
            catch(System.IO.IOException)
            {
                /*if (tryCount < tryLimit)
                {
                    Logger.GetInstance().Debug("IO Exception try reconnect try count:" + tryCount);
                    tryCount++;
                    try
                    {
                        readCount = stream.Read(response, 0, responseLength);
                    }
                    catch
                    {
                        Logger.GetInstance().Debug("SeapClient discard not possible");       
                    }
                    try
                    {
                        Reconnect();
                    }
                    catch
                    {
                        Logger.GetInstance().Debug("REconnect failed");
                    }
                    return sendMessage(msg.TrimEnd());
                }
                else 
                {
                    throw;
                }*/
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}
