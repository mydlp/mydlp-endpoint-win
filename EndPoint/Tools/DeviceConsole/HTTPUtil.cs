using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace MyDLP.EndPoint.Tools.DeviceConsole
{
    class HTTPUtil
    {
           public static int notifyServer(String serverAddress, String idHash, String uniqId, String comment,String model) {
               // encode form data
               StringBuilder postString = new StringBuilder();
               bool first = true;
               KeyValuePair<String, String>[] formValues = {
                   new KeyValuePair<string,string>("deviceid", idHash),
                   new KeyValuePair<string,string>("uniqid", uniqId),
                   new KeyValuePair<string,string>("comment", comment),
                   new KeyValuePair<string,string>("model", model)
               };
               String url = "http://" + serverAddress + "/addtousbwl.php"; 
               foreach (KeyValuePair<String, String> pair in formValues)
               {
                   if (first)
                       first = false;
                   else
                       postString.Append("&");
                   postString.AppendFormat("{0}={1}", pair.Key, System.Web.HttpUtility.UrlEncode(pair.Value));
               }
               ASCIIEncoding ascii = new ASCIIEncoding();
               byte[] postBytes = ascii.GetBytes(postString.ToString());

               // set up request object
               HttpWebRequest request;
               try
               {
                   request = WebRequest.Create(url) as HttpWebRequest;
               }
               catch (UriFormatException)
               {
                   request = null;
               }
               if (request == null)
                   throw new ApplicationException("Invalid URL: " + url);
               request.Method = "POST";
               request.ContentType = "application/x-www-form-urlencoded";
               request.ContentLength = postBytes.Length;
               request.Timeout = 10000;

               // add post data to request
               Stream postStream = request.GetRequestStream();
               postStream.Write(postBytes, 0, postBytes.Length);
               postStream.Close();

               try
               {
                   return (int) ((HttpWebResponse)request.GetResponse()).StatusCode;
               }
               catch
               {
                   throw;
               }
           }
    }
}
