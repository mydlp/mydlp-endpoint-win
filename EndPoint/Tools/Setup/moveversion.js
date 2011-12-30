var config = WScript.Arguments.Item(0);
var pdir = WScript.Arguments.Item(1);

if (config == "Release")
{      
    var rline = "";
    
    fs = new ActiveXObject("Scripting.FileSystemObject");
    f = fs.GetFile(pdir + "ProductVersion.wxi");
    
    is = f.OpenAsTextStream(1, 0);    
    rline = is.ReadLine();
    is.Close();

    var patversion = new RegExp("ProductVersion=\"([0-9]+)\.([0-9]+)\.([0-9]+)\"");
    var version = patversion.exec(rline);
    var fullversion = version[1] + "_" + version[2] + "_" + version[3];    
    
    
    objShell.run("cmd /C \"C:\\WinDDK\7600.16385.1\\bin\x86\SignTool.exe sign /sha1 cb2645d541b0f15ece224fc26c8d713711069e9e /t http://timestamp.globalsign.com/scripts/timestamp.dll bin\\Release\\mydlp.msi\"",0 , true);

    f = fs.GetFile(pdir + "bin\\Release\\mydlp.msi");    
    f.Move(pdir + "bin\\Release\\mydlp_" + fullversion + ".msi"); 
    
    objShell.run("cmd /C \"git.exe tag -a " + newVersion + " -m \"tag for release: " + newVersion + "\"",0 , true);
    objShell.run("cmd /C \"git.exe push --tags \"", 0, true);
}