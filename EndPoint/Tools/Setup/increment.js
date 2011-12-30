var config = WScript.Arguments.Item(0);
var pdir = WScript.Arguments.Item(1);

if (config == "Release")
{   
  var objShell = WScript.CreateObject("WScript.Shell");
  objShell.run("cmd /C \"git.exe describe > " + pdir + "gitoutput.txt\"", 0, true); 
  var fs = new ActiveXObject("Scripting.FileSystemObject");
  var f = fs.GetFile(pdir + "gitoutput.txt");  
  var is = f.OpenAsTextStream(1, 0);    
  var rline = is.ReadLine();
  is.Close();
  fs.DeleteFile(pdir + "gitoutput.txt");

  var versionExp = new RegExp("([0-9]+)\.([0-9]+)\.([0-9]+)");
  var oldTagVersion = versionExp.exec(rline);
  var oldVersion = oldTagVersion[1] + "." + oldTagVersion[2] + "." + oldTagVersion[3]; 
  var newVersion = oldTagVersion[1] + "." + oldTagVersion[2] + "." + ++oldTagVersion[3]; 

  var productVersion= "<?xml version=\"1.0\" encoding=\"utf-8\"?> <Include> <?define ProductVersion=\"0.0.0\"?> </Include>";

  productVersion = productVersion.replace("0.0.0",newVersion);

  try
  {
     f = fs.GetFile(pdir + "ProductVersion.wxi");
     if (f) {f.Delete()};
  }
  catch(err)
  {

  }

  var fs = new ActiveXObject("Scripting.FileSystemObject");
  var f = fs.CreateTextFile(pdir + "ProductVersion.wxi");
  f.WriteLine(productVersion);
  f.Close();
}
