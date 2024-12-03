dotnet publish -p:PublishProfile=FolderProfile

for /f %%a in ('dir bin\Release\publish\*.exe /b') do ( 
	set exe=%%a 
	goto :continue
)
:continue
if not exist "bin\Release\publish\Intune" mkdir "bin\Release\publish\Intune"
del /Q /S bin\Release\publish\Intune
copy bin\Release\publish\*.exe bin\Release\publish\Intune /Y
IntuneWinAppUtil.exe -c bin\Release\publish\Intune -s %exe% -o bin\Release\publish\Intune
exit