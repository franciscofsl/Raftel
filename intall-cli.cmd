@echo off
SETLOCAL

REM Variables configurables
SET PROJECT_PATH=tools\raftel.cli\raftel.cli.csproj
SET PACKAGE_OUTPUT=tools\raftel.cli\bin\Release
SET PACKAGE_ID=Raftel.Cli

echo =======================================================
echo Empaquetando herramienta...
dotnet pack %PROJECT_PATH% -c Release

IF %ERRORLEVEL% NEQ 0 (
    echo Error al empaquetar el proyecto.
    EXIT /B 1
)

echo =======================================================
echo Instalando o actualizando herramienta globalmente...
dotnet tool install --global --add-source %PACKAGE_OUTPUT% %PACKAGE_ID%

IF %ERRORLEVEL% NEQ 0 (
    echo La herramienta ya existe, intentando actualizar...
    dotnet tool update --global --add-source %PACKAGE_OUTPUT% %PACKAGE_ID%
)

echo =======================================================
echo Finalizado.
ENDLOCAL
pause
