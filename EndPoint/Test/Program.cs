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
        static void Main(string[] args)
        {
            MyDLPEP.MiniFilterTestInstaller testInstaller = new MyDLPEP.MiniFilterTestInstaller();
            testInstaller.InstallMiniFilter();
            MyDLP.EndPoint.Service.MainController controller = 
                MyDLP.EndPoint.Service.MainController.GetInstance();
            
            //restart MyDLPMF for testing purpose
            MyDLP.EndPoint.Core.SvcController.RestartService("MyDLPMF",120000);
            controller.Start();

            Console.ReadLine();

           /* bool run = true;
            while (run)
            {
               if( Console.ReadLine() == "q")
                   run = false;
            }*/
        }
    }
}
