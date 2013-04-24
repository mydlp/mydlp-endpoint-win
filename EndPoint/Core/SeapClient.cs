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
using System.Net.Mail;

namespace MyDLP.EndPoint.Core
{
    public class SeapClient
    {
        static SeapClient seapClient = null;
        static SeapClient trapClient = null;
        int port = Configuration.SeapPort;
        String server = Configuration.SeapServer;
        TcpClient client;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;
        BinaryWriter binaryWriter;

        int readTimeout = 145000;
        int writeTimeout = 1800;
        int responseLength = 512;


        public static FileOperation.Action GetWriteDecisionByPath(String filePath, String tempFilePath)
        {
            try
            {
                Logger.GetInstance().Debug("GetWriteDecisionByPath filePath:" + filePath + " tempFilePath:" + tempFilePath);

                if (tempFilePath.Equals("") || Engine.GetShortPath(tempFilePath).Equals(""))
                    return FileOperation.Action.ALLOW;

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

                response = sClient.sendMessage("SETPROP " + id +
                    " fullpath=" + qpEncode(filePath));
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
                Logger.GetInstance().Error(e);
                //todo: Default Acion
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion
            return FileOperation.Action.ALLOW;
        }

        public static FileOperation.Action GetWriteDecisionByCache(String filePath, MemoryStream cache)
        {
            try
            {
                Logger.GetInstance().Debug("GetWriteDecisionByCache path: " + filePath + " length:" + cache.Length);

                if (cache.Length == 0)
                    return FileOperation.Action.ALLOW;

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
                      " fullpath=" + qpEncode(filePath));
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
                Logger.GetInstance().Error(e);
                //todo: Default Acion
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion            
            return FileOperation.Action.ALLOW;
        }

        public static FileOperation.Action GetReadDecisionByPath(String filePath)
        {
            try
            {
                Logger.GetInstance().Debug("GetReadDecisionByPath path: " + filePath);

                String shortFilePath = Engine.GetShortPath(filePath);
                if (filePath.Equals("") || shortFilePath.Equals(""))
                    return FileOperation.Action.ALLOW;

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

                response = sClient.sendMessage("SETPROP " + id +
                    " fullpath=" + qpEncode(filePath));
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
                Logger.GetInstance().Error(e);
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
                Logger.GetInstance().Debug("GetUSBSerialDecision serial: " + serial);

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

                response = sClient.sendMessage("SETPROP " + id + " type=usb_device");

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
                Logger.GetInstance().Error(e);
                //todo: Default Acion
                return FileOperation.Action.ALLOW;
            }
            //todo: Default Acion
            return FileOperation.Action.ALLOW;
        }

        public static bool HasNewConfiguration()
        {
            try
            {
                Logger.GetInstance().Debug("GetConfUpdateNotification");
                SeapClient sClient = SeapClient.GetInstance();
                String response;
                String[] splitResp;

                String loggedOnUser = Configuration.GetLoggedOnUser();
                String version = Configuration.GetMyDLPVersion();
                if (version == null || version.Length == 0)
                    version = "No version";

                response = sClient.sendMessage("CONFUPDATE "
                    + "version=" + qpEncode(version) + " "
                    + "user=" + qpEncode(loggedOnUser) + " "
                    + "hostname=" + qpEncode(System.Environment.MachineName));
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return false;
                }

                if (splitResp[1].Equals("yes"))
                {
                    return true;
                }
                else if (splitResp[1].Equals("no"))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e);
                //todo: Default Acion
                return false;
            }
            //todo: Default Acion
            return false;
        }

        public static string GetKeyfile()
        {
            try
            {
                Logger.GetInstance().Debug("GetKeyfile");
                SeapClient sClient = SeapClient.GetInstance();
                String response;
                String[] splitResp;

                response = sClient.sendMessage("GETKEY");
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return null;
                }

                return splitResp[1];

            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e);
                return null;
            }
            //todo: Default Acion
            return null;
        }


        public static bool HasKeyfile()
        {
            try
            {
                Logger.GetInstance().Debug("HasKeyfile");
                SeapClient sClient = SeapClient.GetInstance();
                String response;
                String[] splitResp;

                response = sClient.sendMessage("HASKEY");
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return false;
                }

                if (splitResp[1].Equals("yes"))
                {
                    return true;
                }
                else if (splitResp[1].Equals("no"))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e);
                //todo: Default Acion
                return false;
            }
            //todo: Default Acion
            return false;
        }

        public static FileOperation.Action NotitfyPrintOperation(String documentName, String printerName, String path)
        {
            try
            {
                SeapClient sClient = SeapClient.GetInstance();
                Logger.GetInstance().Debug("NotitfyPrintOperation " +
                    " documentName: " + documentName + " printerName : " + printerName +
                    " path: " + path);

                String shortFilePath = Engine.GetShortPath(path);

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

                /*response = sClient.sendMessage("SETPROP " + id + " burn_after_reading=true");
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return FileOperation.Action.ALLOW;
                }*/

                /*response = sClient.sendMessage("SETPROP " + id + " pageCount=" + pageCount);
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return;
                }*/

                response = sClient.sendMessage("SETPROP " + id + " filename=" + qpEncode(documentName) + ".xps");
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return FileOperation.Action.ALLOW;
                }

                response = sClient.sendMessage("SETPROP " + id + " printerName=" + qpEncode(printerName));
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

                /*response = sClient.sendMessage("SETPROP " + id + " user=" + userName);
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return;
                }*/

                response = sClient.sendMessage("PUSHFILE " + id + " " +
                 qpEncode(shortFilePath));
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
                Logger.GetInstance().Error(e);
                return FileOperation.Action.ALLOW;
            }
            return FileOperation.Action.ALLOW;
        }

        public static void InitiateRemotePrint(String jobId, String printerName, String server, String path)
        {
            try
            {
                SeapClient sClient = SeapClient.GetInstance();
                Logger.GetInstance().Debug("InitiatedRemotePrint " + " printerName: " + printerName + " server:" + server +
                    " path: " + path);
                String shortFilePath = Engine.GetShortPath(path);
                String response;
                String request = "IECP " + server + " " + qpEncode(shortFilePath)
                    + " command=print"
                    + " user=" + Configuration.GetLoggedOnUser()
                    + " printerName=" + qpEncode(printerName)
                    + " jobId=" + qpEncode(jobId);
                Logger.GetInstance().Debug("request:<" + request + ">");

                response = sClient.sendMessage(request);
                Logger.GetInstance().Debug("response:<" + response + ">");
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e.ToString());
            }
        }

        public delegate void RemotePrinterHandlerDelegate(String jobId, String printJobPath, String printerName);

        public static void ListenRemotePrintBlocking(RemotePrinterHandlerDelegate processPrinting)
        {
            String user = "";
            String printerName = "";
            String jobId = "";
            String path = "";

            try
            {
                SeapClient sClient = SeapClient.GetTrapInstance();
                Logger.GetInstance().Debug("TRAP");
                String response;
                String[] splitResp;

                response = sClient.sendMessage("TRAP");
                splitResp = response.Split(' ');
                if (!splitResp[0].Equals("OK"))
                {
                    return;
                }
                if (splitResp[1].StartsWith("retrap"))
                {
                    return;
                }
                else if (splitResp[1].StartsWith("print"))
                {
                    path = splitResp[2];
                    for (int i = 3; i < splitResp.Length; i++)
                    {
                        int idx = splitResp[i].IndexOf('=');
                        String key = splitResp[i].Substring(0, idx);
                        String val = splitResp[i].Substring(idx + 1);
                        val = qpDecode(val);
                        if (key == "user") user = val;
                        else if (key == "user") user = val;
                        else if (key == "printerName") printerName = val;
                        else if (key == "jobId") jobId = val;
                    }
                    Logger.GetInstance().Info("Handling remte print on server for remote user:" + user +
                        " printerName: " + printerName + " jobId: " + jobId + " path: " + path);
                    processPrinting(jobId, path, printerName);
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e);
            }
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
                Logger.GetInstance().Debug(e);
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
                reader = new StreamReader(stream, System.Text.Encoding.ASCII);
                writer = new StreamWriter(stream, System.Text.Encoding.ASCII);
                binaryWriter = new BinaryWriter(stream);


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
                    Logger.GetInstance().Error("Reconnect unable to close client: " + e);
                }

                client = new TcpClient(server, port);
                stream = client.GetStream();
                stream.ReadTimeout = readTimeout;
                stream.WriteTimeout = writeTimeout;
                reader = new StreamReader(stream, System.Text.Encoding.ASCII);
                writer = new StreamWriter(stream, System.Text.Encoding.ASCII);
                binaryWriter = new BinaryWriter(stream);
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

        public static SeapClient GetTrapInstance()
        {
            try
            {
                if (trapClient == null)
                {
                    trapClient = new SeapClient();
                }
                return trapClient;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public String sendMessage(String cmd, MemoryStream msg)
        {
            int tryCount = 0;
            int tryLimit = 3;
            Exception lastException = new Exception("Unknown exception");
            String respMessage = "";

            Logger.GetInstance().Debug("SeapClient send message: <" + cmd + ">");

            lock (this)
            {
                while (tryCount < tryLimit)
                {
                    try
                    {
                        //clean and discard data on sockect if any
                        if (stream.DataAvailable == true)
                            reader.ReadToEnd();

                        writer.WriteLine(cmd);
                        writer.Flush();

                        if (msg != null)
                        {
                            binaryWriter.Write(msg.GetBuffer(), 0, (int)msg.Length);
                            binaryWriter.Flush();
                        }

                        respMessage = reader.ReadLine().Trim();

                        tryCount = 0;
                        break;
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Debug("IO Exception tryCount:" + tryCount);
                        lastException = e;
                        tryCount = handleStreamError(tryCount);
                    }
                }

                if (tryCount >= tryLimit)
                {
                    Logger.GetInstance().Error("SeapClient connection error, engine unreachable:" + lastException.Message + " " + lastException.StackTrace);
                    SvcController.StopMyDLP();
                    return "";
                }
            }

            Logger.GetInstance().Debug("SeapClient read response:  <" + respMessage + ">");
            return respMessage;
        }

        public String sendMessage(String msg)
        {
            String respMessage = "";
            try
            {
                respMessage = sendMessage(msg, null);
            }
            catch
            {
                throw;
            }

            return respMessage;
        }


        public int handleStreamError(int tryCount)
        {
            try
            {
                //consume &discard error message if any


                if (stream != null && stream.DataAvailable)
                {
                    reader.ReadToEnd();
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
            return tryCount;
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
                else if (curr == 9)
                    ret += "=09";
                else if (curr == 32)
                    ret += "=20";
                else if (33 <= curr && curr <= 126)
                    ret += ((char)curr).ToString();
                else
                    ret += ("=" + BitConverter.ToString(new byte[1] { curr }));
            }
            return ret;
        }

        protected static String qpDecode(String str)
        {
            Attachment a = Attachment.CreateAttachmentFromString("", "=?utf-8?Q?" + str + "?=");
            return a.Name;
        }
    }
}
