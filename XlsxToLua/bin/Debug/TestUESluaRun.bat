@echo off

XlsxToLua.exe UESlua\TestExcel UESlua\ExportLua -noClient -noLang -allowedNullNumber -printEmptyStringWhenLangNotMatching "-exportLangPath(UESlua\ExportLang)"
set errorLevel = %errorlevel%
if errorLevel == 0 (
	@echo 导出Lang成功
) else (
	@echo 导出Lang失败
)


XlsxToLua.exe UESlua\TestExcel UESlua\ExportLua -noClient lang.txt -columnInfo -allowedNullNumber -printEmptyStringWhenLangNotMatching -exportUESLua($all) "-exportUESLuaParam(exportPath=UESlua\ExportSLua|cppAPI=PROJECTZ_API)"
set errorLevel = %errorlevel%
if errorLevel == 0 (
	@echo 导出成功
) else (
	@echo 导出失败
)
pause