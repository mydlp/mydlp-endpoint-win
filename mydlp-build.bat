@set PATH=c:\msysgit\msysgit\bin;c:\msysgit\msysgit\mingw\bin;c:\msysgit\msysgit\cmd;%PATH%

cd C:\hudson\workspace\mydlp-endpoint-win\EndPoint


cmd /c devenv /rebuild "Release|Any CPU" MyDLP.EndPoint.sln