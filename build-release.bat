@echo off
setlocal
cd /d "%~dp0"

if not exist "NoFences.sln" (
    echo Could not find NoFences.sln in this folder.
    pause
    exit /b 1
)

where msbuild >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo MSBuild not found on PATH. Trying common Visual Studio locations...
    set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    if not exist "%MSBUILD%" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
    if not exist "%MSBUILD%" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    if not exist "%MSBUILD%" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    if not exist "%MSBUILD%" (
        echo Unable to locate MSBuild. Install Visual Studio Build Tools or add MSBuild to PATH.
        pause
        exit /b 1
    )
) else (
    for /f "delims=" %%I in ('where msbuild 2^>nul ^| findstr /I /R /C:"MSBuild.exe$"') do set "MSBUILD=%%I"
)

echo Building Release configuration...
"%MSBUILD%" "NoFences.sln" /t:Build /p:Configuration=Release /m
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b %errorlevel%
)

echo Build succeeded.
pause
