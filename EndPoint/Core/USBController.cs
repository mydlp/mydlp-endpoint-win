using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Threading;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace MyDLP.EndPoint.Core
{
    public class USBController
    {
        static ManagementEventWatcher w = null;
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
                try
                {
                    ManagementObjectSearcher theSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
                    globalUsbLockFlag = false;

                    Logger.GetInstance().Debug("Enter GetUSBStorages globalUSbLockFlag:" + globalUsbLockFlag);
                    foreach (ManagementObject currentObject in theSearcher.Get())
                    {
                        String id = currentObject["PNPDeviceID"].ToString();
                        Logger.GetInstance().Debug("USB storage id: " + id);

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

                                if (Core.SeapClient.GetUSBSerialDecision(idHash) != FileOperation.Action.ALLOW)
                                {
                                    Logger.GetInstance().Debug("UsbLockFlag set true:" + globalUsbLockFlag);
                                    globalUsbLockFlag = true;
                                }
                            }

                            catch (Exception e)
                            {
                                Logger.GetInstance().Error(e.Message + e.StackTrace);                           
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Error(e.Message + e.StackTrace);
                    globalUsbLockFlag = false;
                }
            }

            Logger.GetInstance().Debug("UsbLockFlag finall value:" + globalUsbLockFlag);
        }

        public static void AddUSBHandler()
        {
            WqlEventQuery q;

            ManagementScope scope = new ManagementScope("root\\CIMV2");

            scope.Options.EnablePrivileges = true;
            try
            {
                q = new WqlEventQuery();
                q.EventClassName = "__InstanceCreationEvent";
                q.WithinInterval = new TimeSpan(0, 0, 3);
                q.Condition = @"TargetInstance ISA 'Win32_USBControllerdevice'";
                w = new ManagementEventWatcher(scope, q);

                w.EventArrived += new EventArrivedEventHandler(USBInserted);
                w.Start();
            }
            catch (Exception e)
            {
                w.Stop();
            }
        }

        public static void RemoveUSBHandler()
        {
            w.Stop();
            globalUsbLockFlag = false;
        }

        public static void USBInserted(object sender, EventArgs e)
        {
            GetUSBStorages();
        }

    }
}
