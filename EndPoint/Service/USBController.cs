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

        public static void GetUSBStorages()
        {
            try
            {
                ManagementObjectSearcher theSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
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
                                idHash += b.ToString("X");
                            }

                            if (Core.SeapClient.GetUSBSerialDecision(idHash) != FileOperation.Action.ALLOW)
                            {
                                Logger.GetInstance().Debug("Removing usb device :" + uniqID);
                                RegistryKey enumUSBKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USBSTOR\Enum");
                                int count = (int)enumUSBKey.GetValue("Count");
                                string vid = "";
                                string pid = "";
                                for (int i = 0; i < count; i++)
                                {
                                    String usbDeviceString = (String)enumUSBKey.GetValue(i.ToString());
                                    if (usbDeviceString.Contains(uniqID))
                                    {
                                        int startVid = usbDeviceString.IndexOf("Vid_") + 4;
                                        int endVid = usbDeviceString.IndexOf("&", startVid);
                                        vid = usbDeviceString.Substring(startVid, endVid - startVid);
                                        int endPid = usbDeviceString.IndexOf("\\", endVid + 1);
                                        pid = usbDeviceString.Substring(endVid + 5, endPid - endVid - 5);
                                    }
                                }
                                RegistryKey enumUSBDevKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
                                String devNode = "";
                                foreach (String devNodeKeyString in enumUSBDevKey.GetSubKeyNames())
                                {
                                    foreach (String devNodeInstanceString in enumUSBDevKey.OpenSubKey(devNodeKeyString).GetSubKeyNames())
                                    {
                                        if (devNodeInstanceString == uniqID)
                                        {
                                            devNode = devNodeKeyString;
                                            break;
                                        }
                                    }
                                }

                                for (int i = 0; i < 3; i++)
                                {
                                    MyDLPEP.USBRemover.remove("USB\\" + devNode + "\\" + uniqID);
                                    System.Threading.Thread.Sleep(1000);
                                }
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
            }
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
        }

        public static void USBInserted(object sender, EventArgs e)
        {
            GetUSBStorages();
        }

    }
}
