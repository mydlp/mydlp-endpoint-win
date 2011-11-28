echo %cd%
heat dir "..\..\..\..\..\..\mydlp-deployment-env\erl5.8.5" -sreg -cg "erl5.8.5" -ag -dr INSTALLLOCATION -template fragment -var var.erl5_8_5Path -t ../../filter.xsl -o ../../harvested/erl5.8.5.wxs
heat dir "..\..\..\..\..\..\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\mydlp" -srd -sreg -cg "engine_erl" -ag -dr ENGINEERLDIR -template fragment -var var.engine_erlPath -t ../../filter.xsl -o ../../harvested/engine_erl.wxs
heat dir "..\..\..\..\..\..\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\backend\py"  -sreg -cg "engine_py" -ag -dr ENGINEDIR -template fragment -var var.engine_pyPath -t ../../filter.xsl -o ../../harvested/engine_py.wxs
heat dir "..\..\..\..\..\..\mydlp-deployment-env\magic" -sreg -cg "magic" -ag -dr INSTALLLOCATION -template fragment -var var.magicPath -t ../../filter.xsl -o ../../harvested/magic.wxs
heat dir "..\..\..\..\..\..\mydlp-deployment-env\Python26" -sreg -cg "Python26" -ag -dr INSTALLLOCATION -template fragment -var var.Python26Path -t ../../filter.xsl -o ../../harvested/Python26.wxs
heat dir "..\..\..\..\..\..\mydlp-deployment-env\cygwin" -sreg -cg "cygwin" -ag -dr INSTALLLOCATION -template fragment -var var.cygwinPath -t ../../filter.xsl -o ../../harvested/cygwin.wxs
heat dir "..\..\..\..\..\..\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\thrift\gen-py\mydlp" -srd -sreg -cg "mydlp_py" -ag -dr MYDLP_PYDIR -template fragment -var var.mydlp_pyPath -t ../../filter.xsl -o ../../harvested/mydlp_py.wxs
