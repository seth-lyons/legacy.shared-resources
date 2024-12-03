"C:\Program Files (x86)\Windows Kits\10\bin\x86\signtool.exe" sign /f "Temp\NLCCodeSigningCert.pfx" /p {PUTPASSWORDHERE} /tr http://tsa.starfieldtech.com /td SHA256 "Temp\AddVPN.msi"
Pause