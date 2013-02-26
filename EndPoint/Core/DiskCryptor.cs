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
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.ComponentModel;

namespace MyDLP.EndPoint.Core
{
    public class DiskCryptor
    {

        private static string getDCPath()
        {
            string middir = @"x86";
            if (Configuration.IsOS64Bit())
                middir = @"x64";
            return Configuration.AppPath + @"\internal\dcrypt\" + middir;
        }

        private static string getDCCon()
        {
            return "cd " + getDCPath() + " && dccon.exe";
        }

        private static string getDCInst()
        {
            return "cd " + getDCPath() + " && dcinst.exe";
        }

        protected static void installDC()
        {
            string command = getDCInst() + @" -setup";
            ExecuteParameters eparams = new ExecuteParameters(command, "DC install");
            ProcessControl.CommandOutputSync(eparams);
        }

        protected static void configureDC()
        {
            string command = getDCCon() + @" -config-mydlp";
            ExecuteParameters eparams = new ExecuteParameters(command, "DC config mydlp");
            ProcessControl.CommandOutputSync(eparams);
        }

        protected static void deconfigureDC()
        {
            string command = getDCCon() + @" -deconfig-mydlp";
            ExecuteParameters eparams = new ExecuteParameters(command, "DC deconfig mydlp");
            ProcessControl.CommandOutputSync(eparams);
        }

        protected static string getPartitionId(string driveLetter)
        {
            string command = getDCCon() + @" -enum";
            ExecuteParameters eparams = new ExecuteParameters(command, "DC enum");
            string output = ProcessControl.CommandOutputSync(eparams);
            string[] lines = output.Split('\n');
            foreach (string line in lines)
            {
                if (line.Contains("reboot you system"))
                    return null;

                if (line.Contains("is not compatible with the version of Windows"))
                    return null;

                string[] parts = line.Split('|');
                if (parts.Length != 4)
                    continue;
                string drivePart = parts[1];
                if (drivePart.Contains(" " + driveLetter + ": "))
                {
                    string partitionIdPart = parts[0];
                    return partitionIdPart.Trim();
                }
            }
            return null;
        }

        protected static bool isEncrypted(string partitionId)
        {
            string command = getDCCon() + @" -info " + partitionId;
            ExecuteParameters eparams = new ExecuteParameters(command, "DC isEncrypted");
            string output = ProcessControl.CommandOutputSync(eparams);
            string[] lines = output.Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("Cipher:"))
                    return true;
            }
            return false;
        }

        protected static bool doesNeedFormatting(string partitionId)
        {
            string command = getDCCon() + @" -info " + partitionId;
            ExecuteParameters eparams = new ExecuteParameters(command, "DC doesNeedFormatting");
            string output = ProcessControl.CommandOutputSync(eparams);
            string[] lines = output.Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("Status:") &&
                    (line.Contains("boot") || line.Contains("system"))
                    )
                    return false;

                if (line.StartsWith("Device:") && line.Contains(@"\\Device\CdRom"))
                    return false;


                if (line.StartsWith("Cipher:"))
                    return false;

                if (line.Contains("reboot you system"))
                    return false;

                if (line.Contains("is not compatible with the version of Windows"))
                    return false;
            }
            return true;
        }

        protected static bool isMounted(string partitionId)
        {
            string command = getDCCon() + @" -info " + partitionId;
            ExecuteParameters eparams = new ExecuteParameters(command, "DC isMounted");
            string output = ProcessControl.CommandOutputSync(eparams);
            string[] lines = output.Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("Status:"))
                {
                    if (line.Contains(" mounted"))
                        return true;
                    if (line.Contains(" unmounted"))
                        return false;
                }

            }
            return false;
        }

        protected static void formatPartition(string partitionId, string fsType)
        {
            string keyfile = Engine.GetShortPath(SeapClient.GetKeyfile());
            if (File.Exists(keyfile))
            {
                string command = getDCCon() + @" -format " + partitionId + " -q -" + fsType + " -a -p mydlp -kf " + keyfile;
                ExecuteParameters eparams = new ExecuteParameters(command, "DC format");
                ProcessControl.CommandOutputSync(eparams);
                File.Delete(keyfile);
            }
        }

        protected static void mountPartition(string partitionId)
        {
            string keyfile = Engine.GetShortPath(SeapClient.GetKeyfile());
            if (File.Exists(keyfile))
            {
                string command = getDCCon() + @" -mount " + partitionId + " -p mydlp -kf " + keyfile;
                ExecuteParameters eparams = new ExecuteParameters(command, "DC mount");
                ProcessControl.CommandOutputSync(eparams);
                File.Delete(keyfile);
            }
        }

        protected static void unmountPartition(string partitionId)
        {
            string command = getDCCon() + @" -unmount " + partitionId + " -f";
            ExecuteParameters eparams = new ExecuteParameters(command, "DC unmount");
            ProcessControl.CommandOutputSync(eparams);
        }

        protected static void cleanupMemory()
        {
            string command = getDCCon() + @" -clean";
            ExecuteParameters eparams = new ExecuteParameters(command, "DC clean");
            ProcessControl.CommandOutputSync(eparams);
        }

        protected static void mountAllEncryptedPartitions()
        {
            string keyfile = Engine.GetShortPath(SeapClient.GetKeyfile());
            if (File.Exists(keyfile))
            {
                string command = getDCCon() + @" -mountall -p mydlp -kf " + keyfile;
                ExecuteParameters eparams = new ExecuteParameters(command, "DC mountall");
                ProcessControl.CommandOutputSync(eparams);
                File.Delete(keyfile);
            }
        }

        protected static void unmountAllEncryptedPartitions()
        {
            string command = getDCCon() + @" -unmountall -f";
            ExecuteParameters eparams = new ExecuteParameters(command, "DC unmountall");
            ProcessControl.CommandOutputSync(eparams);
        }

        // when reg entry usbstor_encryption 0 -> 1
        public static void StartDcrypt()
        {
            installDC();
            configureDC();
        }

        // when reg entry usbstor_encryption 1 -> 0
        public static void StopDcrypt()
        {
            deconfigureDC();
            unmountAllEncryptedPartitions();
            cleanupMemory();
        }

        // should be called after reiving key. when hasKey state turns 0 to 1.
        public static void AfterKeyReceive()
        {
            mountAllEncryptedPartitions();
        }

        // should be called after losing key. when hasKey state turns 1 to 0.
        public static void AfterKeyLose()
        {
            unmountAllEncryptedPartitions();
            cleanupMemory();
        }

        // should not contain semicolon eg. E
        public static bool DoesDriveLetterNeedsFormatting(string driveLetter)
        {
            mountAllEncryptedPartitions();
            string partitionId = getPartitionId(driveLetter);
            if (partitionId == null) return false;
            return doesNeedFormatting(partitionId);
        }

        // fstype can be: fat , fat32, exfat, ntfs, raw
        // windows shows only: fat32, exfat and ntfs
        public static bool FormatDriveLetter(string driveLetter, string fsType)
        {
            string partitionId = getPartitionId(driveLetter);
            if (partitionId == null) return false;
            formatPartition(partitionId, fsType);
            return true;
        }

    }
}
