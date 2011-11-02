using System;
using System.Collections.Generic;
using System.Text;
using System.Management;
using System.Threading;
using System.Security.Cryptography;

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


                            ManagementObjectCollection partitionCollection = new ManagementObjectSearcher(String.Format(
                                "associators of {{Win32_DiskDrive.DeviceID='{0}'}} " +
                                "where AssocClass = Win32_DiskDriveToDiskPartition",
                                currentObject["DeviceID"])).Get();

                            foreach (ManagementObject partition in partitionCollection)
                            {
                                if (partition != null)
                                {
                                    ManagementObjectCollection logicalCollection = new ManagementObjectSearcher(String.Format(
                                     "associators of {{Win32_DiskPartition.DeviceID='{0}'}} where AssocClass= Win32_LogicalDiskToPartition",
                                     partition["DeviceID"])).Get();

                                    foreach (ManagementObject logical in logicalCollection)
                                    {
                                        if (logical != null)
                                        {
                                            ManagementObjectCollection.ManagementObjectEnumerator volumeEnumerator =
                                                new ManagementObjectSearcher(String.Format(
                                             "select * from Win32_LogicalDisk where Name='{0}'",
                                             logical["Name"])).Get().GetEnumerator();

                                            volumeEnumerator.MoveNext();
                                            ManagementObject volume = (ManagementObject)volumeEnumerator.Current;

                                            // CM_Request_Device_Eject_NoUi((int)USBData.DevInst, IntPtr.Zero, null, 0, 0); 
                                            //Console.WriteLine(volume["DeviceID"].ToString());

                                            MD5 md5 = MD5.Create();
                                            byte[] md5buf = md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(uniqID));

                                            String idHash = "";

                                            foreach (byte b in md5buf)
                                            {
                                                idHash += b.ToString("X");
                                            }

                                            if (Core.SeapClient.GetUSBSerialDecision(idHash) != FileOperation.Action.ALLOW)
                                                MyDLPEP.USBRemover.remove((sbyte)volume["DeviceID"].ToString().ToCharArray()[0]);
                                        }
                                    }
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
