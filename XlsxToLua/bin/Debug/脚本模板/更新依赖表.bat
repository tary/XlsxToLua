@echo off
REM 声明采用UTF-8编码
chcp 65001

@SET UE_UE4Editor=D:\Workspace\UE4\Engine\Binaries\Win64\UE4Editor-Cmd.exe
@SET PROJECT_PATH=PATH_TO_PROJECT\PROJECT_NAME
@SET IMPORT_DST=/Game/Res/Json

@echo 导入时需要关掉UE客户端, 且已经通过 生成.bat 生成过UETableDepondence.csv
@pause
@echo off

for /f "tokens=1,2 delims==" %%a IN (config.ini) do (
	if %%a==IMPORT_DST set IMPORT_DST=%%b
	if %%a==PROJECT_PATH set PROJECT_PATH=%%b
	if %%a==UE_HOME set UE_UE4Editor=%%b\Engine\Binaries\Win64\UE4Editor-Cmd.exe
)

::-factory=CSVImportFactory 
"%UE_UE4Editor%" %PROJECT_PATH%\PROJECT_NAME.uproject -run=ImportAssets -dest=%IMPORT_DST% -source=%CD%/UETableDepondence.csv -nosourcecontrol -importsettings=%cd%/CSVImportConfig.json

@if errorLevel == 0 (
	goto eof
)
pause
:eof