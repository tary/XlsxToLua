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

::read config.ini
for /f "tokens=1,2 delims==" %%a IN (config.ini) do (
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


MKDIR %LUA_TABLE_EXPORT_PATH%\zh

XlsxToLua.exe "%EXCEL_PATH%" %LUA_TABLE_EXPORT_PATH%\zh -noClient %LANG_PATH% zh -columnInfo -allowedNullNumber -printEmptyStringWhenLangNotMatching -exportGroup=(client) %UECPP_PARAMS% 
@set errorLevel = %errorlevel%
@if errorLevel == 0 (
	@echo 导出客户端中文成功
) else (
	@echo 导出失败
)

MKDIR %LUA_TABLE_EXPORT_PATH%\en
XlsxToLua.exe "%EXCEL_PATH%" %LUA_TABLE_EXPORT_PATH%\en -noClient %LANG_PATH% en -columnInfo -allowedNullNumber -printEmptyStringWhenLangNotMatching -exportGroup=(client)

@set errorLevel = %errorlevel%
@if errorLevel == 0 (
	@echo 导出客户端英文成功
) else (
	@echo 导出失败
)

MKDIR %LUA_TABLE_EXPORT_PATH%\ja
XlsxToLua.exe "%EXCEL_PATH%" %LUA_TABLE_EXPORT_PATH%\ja -noClient %LANG_PATH% ja -columnInfo -allowedNullNumber -printEmptyStringWhenLangNotMatching -exportGroup=(client)

@set errorLevel = %errorlevel%
@if errorLevel == 0 (
	@echo 导出客户端日文成功
) else (
	@echo 导出失败
)

XlsxToLua.exe "%EXCEL_PATH%" temp -noClient %LANG_PATH% zh -columnInfo -allowedNullNumber -printEmptyStringWhenLangNotMatching -exportGroup=(server) %EXP_GROUP% %JSON_PARAMS% %GO_PARAMS%

@set errorLevel = %errorlevel%
@if errorLevel == 0 (
	@echo 导出服务器成功
) else (
	@echo 导出服务器失败
)

pause

