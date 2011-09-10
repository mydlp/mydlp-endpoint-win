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

using MyDLP.EndPoint.Core;

namespace MyDLP.EndPoint.Service
{
    public class MainController
    {

        public static MainController GetInstance()
        {

            if (controller == null) {
                controller = new MainController();
            }
            return controller;
        }

        public void Start() 
        {
            try
            {
                SvcController.StartService("MyDLPMF", 60000);
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
            MyDLPEP.FilterListener.getInstance().StartListener();            

        }
        
        public void Stop()
        {
            try
            {
                SvcController.StopService("MyDLPMF", 60000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
        }       

        private static MainController controller = null;
        
        private MainController() { }       
    }
}
