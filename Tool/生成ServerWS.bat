@echo off

REM 声明采用UTF-8编码
chcp 65001

@SET LUA_TABLE_EXPORT_PATH=temp
@SET UE_SLUA_EXPORT_PATH=temp
@SET UE_GO_EXPORT_PATH=temp
@SET UE_JSON_EXPORT_PATH=temp
@SET UE_SLUA_API=WUSHUANG_API
@SET EXCEL_PATH=UESlua\TestExcel
@SET LANG_PATH=UESlua\ExportLang\lang.xlsx
@SET EXP_GROUP=-exportGroup=(server)

::read config.ini
for /f "tokens=1,2 delims==" %%a IN (configws.ini) do (
	::set %%a=%%b
	if %%a==LUA_TABLE_EXPORT_PATH set LUA_TABLE_EXPORT_PATH=%%b
	if %%a==UE_SLUA_EXPORT_PATH set UE_SLUA_EXPORT_PATH=%%b
	if %%a==UE_GO_EXPORT_PATH set UE_GO_EXPORT_PATH=%%b
	if %%a==UE_JSON_EXPORT_PATH set UE_JSON_EXPORT_PATH=%%b
	if %%a==UE_SLUA_API set UE_SLUA_API=%%b
	if %%a==EXCEL_PATH set EXCEL_PATH=%%b
	if %%a==LANG_PATH set LANG_PATH=%%b
)


@SET JSON_PARAMS="-exportJson($all)" "-exportJsonParam(exportPath=%UE_JSON_EXPORT_PATH%|extension=json|isFormat=true|isExportJsonArrayFormat=false|isMapIncludeKeyColumnValue=true)" 
@SET UECPP_PARAMS=-exportUESLua($all) "-exportUESLuaParam(exportPath=%UE_SLUA_EXPORT_PATH%|cppAPI=%UE_SLUA_API%)"
@SET GO_PARAMS=-exportGoPath($all) "-exportGoParam(exportGoPath=%UE_GO_EXPORT_PATH%)"


XlsxToLua.exe "%EXCEL_PATH%" %LUA_TABLE_EXPORT_PATH%\zh -noClient %LANG_PATH% zh -columnInfo -allowedNullNumber -printEmptyStringWhenLangNotMatching %EXP_GROUP% %UECPP_PARAMS% %JSON_PARAMS% %GO_PARAMS%
@set errorLevel = %errorlevel%
@if errorLevel == 0 (
	@echo 导出成功
) else (
	@echo 导出失败
)

pause

