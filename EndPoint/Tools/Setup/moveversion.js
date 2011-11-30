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

    f = fs.GetFile(pdir + "bin\\Release\\mydlp.msi");    
    f.Move(pdir + "bin\\Release\\mydlp_" + fullversion + ".msi");
}