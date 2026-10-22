@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ===================================================
echo   KIEM TRA VA DAY CODE LEN GIT (QUAN LY THU VIEN)
echo ===================================================
echo.
echo Thu muc hien tai: %CD%
echo Remote hien tai:
git remote -v
echo.

echo [1/4] Chay dotnet build va dotnet test...
dotnet test Library.sln
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [LOI] Build hoac Test that bai! Vui long kiem tra lai truoc khi push.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [2/4] Them toan bo file thay doi (git add .)...
git add .
git status -s

echo.
echo [3/4] Tao commit...
git commit -m "chore(audit): add system audit reports and fix P0 critical bugs (BUG-001 to BUG-004)"

echo.
echo [4/4] Day code len nhanh main (git push origin main)...
git push -u origin main

echo.
echo Hoan tat!
pause
