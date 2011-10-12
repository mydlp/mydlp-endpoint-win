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

namespace MyDLP.EndPoint.Core
{
    public class FileOperationTableEntry
    {
      

        public OpenFileOperation open { get; set; }
        public WriteFileOperation write { get; set; }

        public FileOperationTableEntry(OpenFileOperation fop)
        {
            this.open = fop;
        }

        public FileOperationTableEntry(WriteFileOperation fop)
        {
            this.write = fop;
        }
             
        public FileOperation.Action Update(OpenFileOperation fop)
        {
            if (open == null)
            {
                open = fop;
                Console.WriteLine("here1");
                //return open.DecideAction();    
            }
            else 
            {

                if (fop.date - open.date > new TimeSpan(0, 0, OpenFileOperation.suppressOpenInterval))
                {
                    open.date = fop.date;
                 //   return open.DecideAction();                    
                }
            }
            return open.DecideAction();
        }

        public void Update(WriteFileOperation fop)
        {
            if (write == null)
            {
                write = fop;
            }
            else
            {
                write.date = write.date;
            }

        }

        public override string ToString()
        {
            String result = "";
            if (open != null) 
            {
                result += open.ToString();
            }  
            if (write != null) 
            {
                if (open != null) 
                {
                    result += "\n";    
                }
                result += write.ToString();
            }
            return result;
        }
    }    
}
