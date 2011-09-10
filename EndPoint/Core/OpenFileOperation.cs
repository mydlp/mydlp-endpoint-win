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
    public class OpenFileOperation : FileOperation
    {
        public const int suppressOpenInterval = 30;
        FileOperation.Action action = FileOperation.Action.UNDEFINED;
        public OpenFileOperation(String path, DateTime date){
            type = FileOperation.OperationType.OPEN;
            this.path = path;
            this.date = date;
        }

        public override string ToString()
        {
            return "OPEN " + "path: " + path + " date: " + date;
        }

        public FileOperation.Action DecideAction() 
        {
            Console.WriteLine("DecideAction action");
            if (action != FileOperation.Action.UNDEFINED)
            {
                Console.WriteLine(action);
                return action;
            }
            try
            {
                action = SeapClient.GetReadDecisionByPath(path);        
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception" + e.Message + e.StackTrace);
                return Action.ALLOW;
            }
            return action;
        }
    }
}
