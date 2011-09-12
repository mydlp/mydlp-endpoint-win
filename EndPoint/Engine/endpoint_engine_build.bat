set pwd=%CD%
cmd /K "cd mydlp\src\thrift && Make.bat && cd ..\..\.. && exit" 
cmd /K "cd mydlp\src\mydlp && Make.bat && cd ..\..\.. && exit" 