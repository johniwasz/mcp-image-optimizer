
REM Requires npm: https://nodejs.org/en/download/
REM On Windows 10, 11 run the following as Administrator:
REM		choco install nodejs
REM
REM By default, npm.ps1 is blocked by Windows PowerShell execution policy. If you trust the download source, then run:
REM		Unblock-File -Path "C:\Program Files\nodejs\npm.ps1"
REM		Unblock-File -Path "C:\Program Files\nodejs\npx.ps1"
REM Or set the execution policy for the current user:
REM		Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
REM 
REM Uses https://github.com/modelcontextprotocol/inspector
REM If using Visual Studio 2022 then 17.14 or higher is required.
npx @modelcontextprotocol/inspector dotnet run --configuration Debug --no-build