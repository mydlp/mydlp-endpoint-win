using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Management;
using System.Threading;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace MyDLP.EndPoint.Core
{
    public class USBController
    {
        //USBSerialCache: key=idHash, value=isBlocked
        static Hashtable USBSerialCache = null;
        static bool active = false;

        public static Boolean globalUsbLockFlag = false;

        public static Boolean IsUsbBlocked()
        {
            lock (typeof(USBController))
            {
                return globalUsbLockFlag && Configuration.UsbSerialAccessControl;
            }
        }

        public static void GetUSBStorages()
        {
            lock (typeof(USBController))
            {
                //If USB Serial Access is not active do nothing
                if (!active)
                {
                    globalUsbLockFlag = false;
                    return;
                }

                try
                {
                    Logger.GetInstance().Debug("GetUSBStorages()");
                    globalUsbLockFlag = false;
                    ManagementObjectSearcher theSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
                    foreach (ManagementObject currentObject in theSearcher.Get())
                    {
                        String id = "";

                        if (currentObject["PNPDeviceID"] != null)
                        {
                            id = currentObject["PNPDeviceID"].ToString();
                            Logger.GetInstance().Debug("USB storage id: " + id);
                        }
                        else
                        {
                            Logger.GetInstance().Info("USB device does not provide PNPDeviceID");
                            continue;
                        }

                        if (id.StartsWith("USBSTOR"))
                        {
                            try
                            {
                                int start = id.LastIndexOf("\\") + 1;
                                int lentgh = id.LastIndexOf("&") - start;
                                String uniqID = id.Substring(start, lentgh);
                                Logger.GetInstance().Debug("USB storage uniq id: " + uniqID);
                                MD5 md5 = MD5.Create();
                                byte[] md5buf = md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(uniqID));

                                String idHash = "";

                                foreach (byte b in md5buf)
                                {
                                    idHash += b.ToString("X2");
                                }

                                if (USBSerialCache.ContainsKey(idHash))
                                {
                                    Logger.GetInstance().Debug("USBSerialCache contains:" + idHash);
                                    if ((bool)USBSerialCache[idHash] == true)
                                    {
                                        Logger.GetInstance().Debug("UsbLockFlag set true for:" + idHash);
                                        globalUsbLockFlag = true;
                                    }
                                    else
                                    {
                                        Logger.GetInstance().Debug("UsbLockFlag not set for:" + idHash);
                                    }
                                }
                                else
                                {
                                    if (Core.SeapClient.GetUSBSerialDecision(idHash) != FileOperation.Action.ALLOW)
                                    {
                                        Logger.GetInstance().Debug("UsbLockFlag set true");
                                        globalUsbLockFlag = true;
                                        Logger.GetInstance().Debug("UsbLockFlag set true for:" + idHash);
                                        USBSerialCache.Add(idHash, true);
                                    }
                                    else
                                    {
                                        USBSerialCache.Add(idHash, false);
                                        Logger.GetInstance().Debug("UsbLockFlag not set for:" + idHash);
                                    }
                                }
                            }

                            catch (Exception e)
                            {
                                Logger.GetInstance().Error(e);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Error(e);
                    USBSerialCache = new Hashtable();
                    globalUsbLockFlag = false;
                }
            }

            Logger.GetInstance().Debug("UsbLockFlag final value:" + globalUsbLockFlag);
        }

        public static void Activate()
        {

            InvalidateCache();
            lock (typeof(USBController))
            {
                active = true;
            }
        }

        public static void Deactive()
        {
            lock (typeof(USBController))
            {
                active = false;
                globalUsbLockFlag = false;
            }
        }

        public static void InvalidateCache()
        {
            lock (typeof(USBController))
            {
                Logger.GetInstance().Debug("Invalidate USBSerialCache");
                USBSerialCache = new Hashtable();
            }
        }
    }
}
