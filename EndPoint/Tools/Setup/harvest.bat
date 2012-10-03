echo %cd%
heat dir "..\..\..\..\..\..\mydlp-deployment-env\erl5.8.5" -sreg -cg "erl5.8.5" -ag -dr INSTALLLOCATION -template fragment -var var.erl5_8_5Path -t ../../filter.xsl -o ../../harvested/erl5.8.5.wxs
heat dir "..\..\..\..\..\..\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\mydlp" -srd -sreg -cg "engine_erl" -ag -dr ENGINEERLDIR -template fragment -var var.engine_erlPath -t ../../filter.xsl -o ../../harvested/engine_erl.wxs
heat dir "..\..\..\..\..\..\mydlp-endpoint-win\EndPoint\Service\printing" -srd -sreg -cg "printing" -ag -dr PRINTINGDIR -template fragment -var var.printingPath -t ../../filter.xsl -o ../../harvested/printing.wxs
heat dir "..\..\..\..\..\..\mydlp-deployment-env\jre7" -sreg -cg "jre7" -ag -dr INSTALLLOCATION -template fragment -var var.jre7Path -t ../../filter.xsl -o ../../harvested/jre7.wxs
heat dir "..\..\..\..\..\..\mydlp-deployment-env\cygwin" -sreg -cg "cygwin" -ag -dr INSTALLLOCATION -template fragment -var var.cygwinPath -t ../../filter.xsl -o ../../harvested/cygwin.wxs
heat dir "..\..\..\..\..\..\mydlp-deployment-env\internal" -sreg -cg "internal" -ag -dr INSTALLLOCATION -template fragment -var var.internalPath -t ../../filter.xsl -o ../../harvested/internal.wxs
