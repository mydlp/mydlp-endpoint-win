var config = WScript.Arguments.Item(0);
var pdir = WScript.Arguments.Item(1);

if (config == "Release")
{
    var objShell = WScript.CreateObject("WScript.Shell");
    objShell.run("cmd /C \" cd  " + pdir +  "..\\..\\..\\..\\mydlp-development-env\\pp && processmsi.bat " + pdir + "bin\\Release\\MyDLP.EndPoint.Tools.ControlPanel.exe\"",1 , true);  

}