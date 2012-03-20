set pwd=%CD%

cmd /K "C:\WinDDK\7600.16385.1\bin\selfsign\inf2cat /driver:C:\workspace\mydlp-endpoint-win\EndPoint\Service\Printing\ /os:7_X86,7_X64,XP_X86 && exit"

cmd /K "cd %pwd% && cd "..\..\..\..\mydlp-development-env\pp" && processprintdrv.bat %pwd% && exit" 