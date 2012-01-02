set pwd=%CD%
cmd /K "C:\WinDDK\7600.16385.1\bin\setenv.bat C:\WinDDK\7600.16385.1\ chk x86 WXP no_oacr && cd %pwd% && cd src && build -ceZ && cd .. && copy MyDLPMiniFilter.inf src\objchk_wxp_x86\i386\ && exit" 
cmd /K "C:\WinDDK\7600.16385.1\bin\setenv.bat C:\WinDDK\7600.16385.1\ chk x64 Win7 no_oacr && cd %pwd% && cd src && build -ceZ && cd .. && copy MyDLPMiniFilter.inf src\objchk_win7_amd64\amd64\ && exit" 

cmd /K "cd %pwd% && cd "..\..\..\mydlp-development-env\pp" && processfilter.bat %pwd% && exit" 