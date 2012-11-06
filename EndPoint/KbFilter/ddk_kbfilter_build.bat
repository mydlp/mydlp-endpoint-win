set pwd=%CD%
cmd /K "C:\WinDDK\7600.16385.1\bin\setenv.bat C:\WinDDK\7600.16385.1\ chk x86 WXP no_oacr && cd %pwd% && cd src && build -ceZ && exit" 
cmd /K "C:\WinDDK\7600.16385.1\bin\setenv.bat C:\WinDDK\7600.16385.1\ chk x64 Win7 no_oacr && cd %pwd% && cd src && build -ceZ && exit" 

cmd /K "cd %pwd% && cd "..\..\..\mydlp-development-env\pp" && processkbfilter.bat %pwd% && exit" 