@echo off
echo =============================
echo Apo´s Executor - Build Script
echo =============================
echo.

echo [*] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo [!] Restore failed!
    pause
    exit /b %errorlevel%
)

echo.
echo [*] Building Release (x64)...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
if %errorlevel% neq 0 (
    echo [!] Build failed!
    pause
    exit /b %errorlevel%
)

echo.
echo [+] Build successful!
echo [+] Output: bin\Release\net6.0-windows\win-x64\publish\FiveM-AntiCheat-Executor.exe
echo.

pause
