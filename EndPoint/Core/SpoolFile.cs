using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace MyDLP.EndPoint.Core.Print
{
  
    class SpoolFile
    {
        public enum FileType { PS, PCL, EMF, GDI, ZIMF, UNKNOWN };
        public string Path { get; set; }        
        public string Extension { get; set; }   
        public FileType SpoolType { get; set; }

        Regex pjlLangRegex;

        public SpoolFile()
        {
            SpoolType = FileType.UNKNOWN;
            Extension = ".SPL";
        }

        public SpoolFile(string path)
        {
            pjlLangRegex = new Regex(@"@PJL ENTER LANGUAGE\=(?<PJL_LANG>(\w)+)", RegexOptions.Multiline);
            this.Path = path;
            SpoolType = FindType(Path);
            switch (SpoolType) 
            {
                case FileType.EMF: 
                    Extension = ".emf";
                    break;
                case FileType.GDI:
                    Extension = ".gdi";
                    break;
                case FileType.PCL:
                    Extension = ".pcl";
                    break;
                case FileType.PS:
                    Extension = ".ps";
                    break;
                case FileType.ZIMF:
                    Extension = ".zimf";
                    break;      
                default:
                    Extension = ".SPL";
                    break;
            }
        }
        private FileType FindType(string path)
        {
            FileType type = FileType.UNKNOWN;
            StreamReader fs = null;

            try
            {
                char[] buffer = new Char[1024];

                fs = new StreamReader(path, Encoding.ASCII);
                fs.Read(buffer, 0, 1024);

                string head = new String(buffer);

                if (head.StartsWith("ZIMF"))
                {
                    type = FileType.ZIMF;
                }
                else if (head.StartsWith("%!PS-Adobe-3.0")) 
                {
                    type = FileType.PS;
                }
                else if (head.StartsWith((char)27 + "%-12345X"))
                {
                    Console.WriteLine("PJL");
                    Match match = pjlLangRegex.Match(head);

                    for (; head.Contains("PJL") && !match.Success; )
                    {
                        //go until pjl sequence finishes or enter language found
                        fs.Read(buffer, 0, 1024);
                        head = new String(buffer);
                        match = pjlLangRegex.Match(head);
                    }

                    if (match.Groups["PJL_LANG"].ToString() == "GDI")
                    {
                        type = FileType.GDI;
                    }
                    else if (match.Groups["PJL_LANG"].ToString() == "POSTSCRIPT")
                    {
                        type = FileType.PS;
                    }

                    else if (match.Groups["PJL_LANG"].ToString().StartsWith("PCL"))
                    {
                        type = FileType.PCL;
                    }
                    else
                    {
                        type = FileType.UNKNOWN;
                    }
                }
                else if (head.Contains("EMF"))
                {
                    type = FileType.EMF;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message, e.StackTrace);
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
            }
            return type;
        }
    }
}
