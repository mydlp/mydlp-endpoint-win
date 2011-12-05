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
    public class SeapClient
    {
        static SeapClient seapClient = null;
        int port = Configuration.SeapPort;
        String server = Configuration.SeapServer;
        TcpClient client;
        NetworkStream stream;
        int readTimeout = 1800;
        int writeTimeout = 1800;
        int responseLength = 512;


        public static FileOperation.Action GetWriteDecisionByPath(String filePath, String tempFilePath)
        {

            if (tempFilePath.Equals("") || Engine.GetShortPath(tempFilePath).Equals(""))
                return FileOperation.Action.ALLOW;

            try
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
                response = sClient.sendMessage("SETPROP " + id +
                        " filename=" + qpEncode(Path.GetFileName(filePath)));
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

                response = sClient.sendMessage("SETPROP " + id +
                    " user=" + Configuration.GetLoggedOnUser());
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return FileOperation.Action.ALLOW;
                }

                response = sClient.sendMessage("PUSHFILE " + id + " " + qpEncode(tempFilePath));

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
            catch (Exception e)
            {
                Logger.GetInstance().Debug(e.Message);
                //todo: Default Acion
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion
            return FileOperation.Action.ALLOW;
        }

        public static FileOperation.Action GetWriteDecisionByCache(String filePath, MemoryStream cache)
        {
            //Logger.GetInstance().Debug("GetWriteDecisionByCache path: " + filePath +" length:" + cache.Length);
            try
            {
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

                response = sClient.sendMessage("SETPROP " + id +
                        " filename=" + qpEncode(Path.GetFileName(filePath)));
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return FileOperation.Action.ALLOW;
                }

                response = sClient.sendMessage("SETPROP " + id +
                    " user=" + Configuration.GetLoggedOnUser());
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
            }
            catch (Exception e)
            {
                Logger.GetInstance().Debug(e.Message);
                //todo: Default Acion
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion            
            return FileOperation.Action.ALLOW;
        }

        public static FileOperation.Action GetReadDecisionByPath(String filePath)
        {
            String shortFilePath = Engine.GetShortPath(filePath);
            if (filePath.Equals("") || shortFilePath.Equals(""))
                return FileOperation.Action.ALLOW;

            try
            {
                SeapClient sClient = SeapClient.GetInstance();
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

                response = sClient.sendMessage("PUSHFILE " + id + " " +
                    qpEncode(shortFilePath));
                //qpEncode(filePath));
                //response = sClient.sendMessage("PUSHFILE " + id + " " + filePath);
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return FileOperation.Action.ALLOW;
                }

                //response = sClient.sendMessage("SETPROP " + id + " filename=" + shortFilePath);
                response = sClient.sendMessage("SETPROP " + id +
                    " filename=" + qpEncode(Path.GetFileName(filePath)));
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

                response = sClient.sendMessage("SETPROP " + id +
                    " user=" + Configuration.GetLoggedOnUser());
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
            catch (Exception e)
            {
                Logger.GetInstance().Debug(e.Message);
                //todo: Default Acion
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion
            return FileOperation.Action.ALLOW;
        }
        
        public static FileOperation.Action GetUSBSerialDecision(String serial)
        {
          
            try
            {
                SeapClient sClient = SeapClient.GetInstance();
                Logger.GetInstance().Debug("GetUSBSerialDecision serial: " + serial);

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

                response = sClient.sendMessage("SETPROP " + id + " type=usb_device");
              
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return FileOperation.Action.ALLOW;
                }

                //response = sClient.sendMessage("SETPROP " + id + " filename=" + shortFilePath);
                response = sClient.sendMessage("SETPROP " + id + " device_id=" + serial);

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
            catch (Exception e)
            {
                Logger.GetInstance().Debug(e.Message);
                //todo: Default Acion
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion
            return FileOperation.Action.ALLOW;
        }

        public static bool SeapConnectionTest()
        {

            try
            {
                SeapClient sClient = SeapClient.GetInstance();
                Logger.GetInstance().Debug("Connection test");

                String response;
                String[] splitResp;
           
                response = sClient.sendMessage("BEGIN");
                                
            }
            catch (Exception e)
            {
                Logger.GetInstance().Debug(e.Message);
                return false;
            }
            return true;
        }


        private SeapClient()
        {
            try
            {
                Logger.GetInstance().Info("Initialize seap client server: " + server + " port: " + port);
                client = new TcpClient(server, port);
                stream = client.GetStream();
                stream.ReadTimeout = readTimeout;
                stream.WriteTimeout = writeTimeout;
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
                Logger.GetInstance().Debug("Reconnect seap client server: " + server + " port: " + port);
                try
                {
                    client.Close();
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Error("Reconnect unable to close client: " + e.Message + " " + e.StackTrace);
                }

                client = new TcpClient(server, port);
                stream = client.GetStream();
                stream.ReadTimeout = readTimeout;
                stream.WriteTimeout = writeTimeout;
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
            Byte[] cmdBin = System.Text.Encoding.ASCII.GetBytes(cmd);
            Byte[] end = System.Text.Encoding.ASCII.GetBytes("\r\n");
            Logger.GetInstance().Debug("SeapClient send message: <" + cmd + ">");
            Byte[] response = new Byte[responseLength];
            int readCount = 0;
            int tryCount = 0;
            int tryLimit = 3;

            lock (seapClient)
            {
                while (tryCount < tryLimit)
                {
                    try
                    {
                        //clean and discard data on sockect if any
                        if (stream.DataAvailable == true)
                            stream.Read(response, 0, responseLength);

                        stream.Write(cmdBin, 0, cmdBin.Length);
                        stream.Write(end, 0, end.Length);
                        stream.Write(msg.GetBuffer(), 0, (int)msg.Length);
                        stream.Flush();
                        readCount = stream.Read(response, 0, responseLength);
                        tryCount = 0;
                        break;
                    }
                    catch (System.IO.IOException e)
                    {
                        Logger.GetInstance().Debug("IO Exception try reconnect try count:" + tryCount);
                        //consume &discard error message if any
                        try
                        {
                            if (stream.DataAvailable)
                            {
                                readCount = stream.Read(response, 0, responseLength);
                            }
                        }
                        catch
                        {
                            Logger.GetInstance().Debug("SeapClient discard not possible");
                        }
                        try
                        {
                            tryCount++;
                            Reconnect();
                        }
                        catch
                        {
                            Logger.GetInstance().Debug("Reconnect failed");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Debug("SendMessage Exception " + e.Message + e.StackTrace);
                        tryCount++;
                        Reconnect();
                    }
                }
            }

            if (tryCount >= tryLimit)
            {
                throw new IOException("SeapClient can not connect to SeapServer after " + tryCount + " attempt, giving up");
            }

            String respMessage = System.Text.Encoding.ASCII.GetString(response, 0, readCount);
            Logger.GetInstance().Debug("SeapClient read response:  <" + respMessage.Trim() + ">");
            return respMessage.Trim();
        }

        public String sendMessage(String msg)
        {
            int readCount = 0;
            int tryCount = 0;
            int tryLimit = 3;

            Logger.GetInstance().Debug("SeapClient send message: <" + msg + ">");
            Byte[] response = new Byte[responseLength];
            msg = msg + "\r\n";
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(msg);

            lock (seapClient)
            {
                while (tryCount < tryLimit)
                {
                    try
                    {
                        //clean and discard data on sockect if any
                        if (stream.DataAvailable == true)
                            stream.Read(response, 0, responseLength);
                        stream.Write(data, 0, msg.Length);
                        stream.Flush();
                        readCount = stream.Read(response, 0, responseLength);
                        tryCount = 0;
                        break;
                    }
                    catch (System.IO.IOException e)
                    {
                        Logger.GetInstance().Debug("IO Exception try reconnect try count:" + tryCount);
                        //consume &discard error message if any
                        try
                        {
                            if (stream.DataAvailable)
                            {
                                readCount = stream.Read(response, 0, responseLength);
                            }
                        }
                        catch
                        {
                            Logger.GetInstance().Debug("SeapClient discard not possible");
                        }
                        try
                        {
                            tryCount++;
                            Reconnect();
                        }
                        catch
                        {
                            Logger.GetInstance().Debug("Reconnect failed");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Debug("SendMessage Exception " + e.Message + e.StackTrace);
                        tryCount++;
                        Reconnect();
                    }
                }
            }

            if (tryCount >= tryLimit)
            {
                throw new IOException("SeapClient can not connect to SeapServer after " + tryCount + " attempt, giving up");
            }

            String respMessage = System.Text.Encoding.ASCII.GetString(response, 0, readCount);
            Logger.GetInstance().Debug("SeapClient read response:  <" + respMessage.Trim() + ">");
            return respMessage.Trim();
        }

        protected static String qpEncode(String inStr)
        {
            byte[] utfBytes = System.Text.Encoding.UTF8.GetBytes(inStr);
            return _qpEncode(utfBytes);
        }

        protected static String _qpEncode(byte[] utfBytes)
        {
            String ret = "";
            for (int i = 0; i < utfBytes.Length; i++)
            {
                byte curr = utfBytes[i];
                if (curr == 61)
                    ret += "=3D";
                else if (curr == 9 || //tab
                            curr == 32 || //space
                            (33 <= curr && curr <= 126))
                    ret += ((char)curr).ToString();
                else
                    ret += ("=" + BitConverter.ToString(new byte[1] { curr }));
            }
            return ret;
        }

    }
}
