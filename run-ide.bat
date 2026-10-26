@echo off
setlocal
cd /d "%~dp0"

rem --- .NET runtime on PATH (matches the project's known location) ---
if exist "C:\Program Files\dotnet\dotnet.exe" (
    set "DOTNET_ROOT=C:\Program Files\dotnet"
    set "PATH=C:\Program Files\dotnet;%PATH%"
)

rem --- 1. Check Node.js availability ---
where node >nul 2>&1
if errorlevel 1 (
    echo.
    echo ERROR: Node.js is not installed or not in PATH.
    echo The Axiom IDE requires Node.js. Download it from https://nodejs.org/
    echo After installing, restart this script.
    pause
    exit /b 1
)

rem --- 2. Build the Axiom CLI (Release) if axiom.exe is missing or built from another
rem     git revision (freshness guard: a stale Release binary must never serve
rem     outdated diagnostics).
node "%~dp0ide\scripts\ensure-axiom.js"
if errorlevel 1 (
    echo.
    echo CLI build FAILED.
    pause
    exit /b 1
)

rem --- 2b. Build/refresh the shared DesktopHost service process when missing or stale
rem     (freshness guard: a missing host is spawn ENOENT on the first backend request, a
rem     host built from older sources is rejected with HOST_STALE).
node "%~dp0ide\scripts\ensure-desktop-host.js"
if errorlevel 1 (
    echo.
    echo DesktopHost build FAILED.
    pause
    exit /b 1
)

rem --- 3. Install/update IDE dependencies when package.json changed after a git pull
rem     (dependency freshness guard: node_modules existing is NOT enough — a pull that
rem     adds a dependency once silently broke the console for exactly that reason).
cd /d "%~dp0ide"
node "%~dp0ide\scripts\ensure-deps.js"
if errorlevel 1 (
    echo.
    echo npm dependency install FAILED.
    pause
    exit /b 1
)

rem --- 4. Build Monaco ESM runtime next to index.html ---
call npm run prepare
if errorlevel 1 (
    echo.
    echo Monaco prepare FAILED.
    pause
    exit /b 1
)

rem --- 5. Launch ---
echo Starting Axiom IDE...
call npx electron .
endlocal
