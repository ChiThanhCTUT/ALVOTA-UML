@echo off
chcp 65001 >nul
cd /d "%~dp0"

set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
set "PATH=%USERPROFILE%\.dotnet;%USERPROFILE%\.dotnet\tools;C:\Program Files\Git\cmd;C:\Program Files (x86)\Git\cmd;%LOCALAPPDATA%\Programs\Git\cmd;%PATH%"

echo ===================================================
echo   KIEM TRA VA DAY CODE LEN GIT (QUAN LY THU VIEN)
echo ===================================================
echo.

where git >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [THONG BAO] Chua tim thay Git trong PATH. Dang tu dong cai dat Git qua winget...
    winget install --id Git.Git -e --source winget --accept-package-agreements --accept-source-agreements
    set "PATH=C:\Program Files\Git\cmd;%LOCALAPPDATA%\Programs\Git\cmd;%PATH%"
)

where git >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [LOI] Khong tim thay Git! Vui long tai Git tai: https://git-scm.com/download/win
    pause
    exit /b 1
)

echo Thu muc hien tai: %CD%
echo Remote hien tai:
git remote -v
echo.

echo [1/4] Don dep 5 file mau UML thua o thu muc goc (BUG-017)...
del /q "01_SimpleClass.cs" "02_Inheritance.cs" "03_Interface.cs" "04_AssociationAndCollection.cs" "05_NestedClass.cs" 2>nul

echo [2/4] Chay dotnet build va dotnet test...
dotnet test Library.sln
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [LOI] Build hoac Test that bai! Vui long kiem tra lai truoc khi push.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo [3/4] Them toan bo file thay doi (git add .)...
git add .
git status -s

echo.
echo [4/4] Tao commit va day code len nhanh main...
git commit -m "fix(all): resolve P1, P2, P3 bugs (BUG-005 to BUG-017) and add integration tests"
git push -u origin main

echo.
echo Hoan tat!
pause
