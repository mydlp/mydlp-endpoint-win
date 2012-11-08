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

namespace MyDLP.EndPoint.Test
{
    class Program
    {
        static MyDLP.EndPoint.Service.MainController controller;

        static void Main(string[] args)
        {
            controller = MyDLP.EndPoint.Service.MainController.GetInstance();

            controller.Start();

            //block until input event to mimic service            
            string c = "";
            while (c != "e")
            {
                c = Console.ReadLine();
                c = c.Trim();
                if (c == "a")
                    Core.USBController.globalUsbLockFlag = false;
                if (c == "b")
                    Core.USBController.globalUsbLockFlag = true;

            }
            controller.Stop();
        }
    }
}
