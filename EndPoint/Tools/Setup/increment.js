var config = WScript.Arguments.Item(0);
var pdir = WScript.Arguments.Item(1);

if (config == "Release")
{   
  var objShell = WScript.CreateObject("WScript.Shell");
  objShell.run("cmd /C \"git.exe describe --tags > " + pdir + "gitoutput.txt\"");

  
  var fs = new ActiveXObject("Scripting.FileSystemObject");
  var f = fs.GetFile(pdir + "gitoutput.txt");  
  var is = f.OpenAsTextStream(1, 0);    
  var rline = is.ReadLine();
  is.Close();
  sleep(3000);
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

  objShell.run("cmd /C \"git.exe tag " + newVersion + "\"");
  objShell.run("cmd /C \"git.exe push --tags \"");
}

//if (config == "Release")
/*{      
    var productVersion= "<?xml version=\"1.0\" encoding=\"utf-8\"?> <Include> <?define ProductVersion=\"0.0.0\"?> </Include>";
    
    var shell = WScript.CreateObject("WScript.shell"); 
    var exec = shell.Exec("%comspec% /K dir");   
    WScript.StdOut.Write(exec.StdOut.ReadALl());    */
    //WScript.Echo("del " + pdir + "ProductVersion.wxi");    
    //shell.run("del " + pdir + "ProductVersion.wxi"); 
    /*     
    var patversion = new RegExp("ProductVersion=\"([0-9]+)\.([0-9]+)\.([0-9]+)\"");
    var version = patversion.exec(rline);
    var oldversion = version[1] + "." + version[2] + "." + version[3];    
    var newversion =  version[1] + "." + version[2] + "." + ++version[3]; 
    
    rline = rline.replace(oldversion,newversion);
    WScript.Echo(rline);*/
    /*
    f = fs.CreateTextFile(pdir + "ProductVersionNew.wxi");
    f.WriteLine(rline);
    f.Close();

    f = fs.GetFile(pdir + "ProductVersion.wxi");
    f.Delete(true);

    f = fs.GetFile(pdir + "ProductVersionNew.wxi");
    f.Move(pdir + "ProductVersion.wxi");
    */
//}

function sleep(milliseconds) {
  var start = new Date().getTime();
  for (var i = 0; i < 1e7; i++) {
    if ((new Date().getTime() - start) > milliseconds){
      break;
    }
  }
}

