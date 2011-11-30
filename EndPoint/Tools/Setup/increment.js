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
    var oldversion = version[1] + "." + version[2] + "." + version[3];    
    var newversion =  version[1] + "." + version[2] + "." + ++version[3]; 

    rline = rline.replace(oldversion,newversion);

    f = fs.CreateTextFile(pdir + "ProductVersionNew.wxi");
    f.WriteLine(rline);
    f.Close();

    f = fs.GetFile(pdir + "ProductVersion.wxi");
    f.Delete(true);

    f = fs.GetFile(pdir + "ProductVersionNew.wxi");
    f.Move(pdir + "ProductVersion.wxi");
}