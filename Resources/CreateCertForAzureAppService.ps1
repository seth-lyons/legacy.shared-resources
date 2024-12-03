
$notAfter = (get-date).AddYears(10)
$appid = "c27a95bf-3755-4ab9-951c-eba89bf8007d"
$cert=New-SelfSignedCertificate -Subject "CN=$appid" -CertStoreLocation "Cert:\LocalMachine\My"  -KeyExportPolicy Exportable -KeySpec Signature -NotAfter $notAfter
Write-Host "Thumbprint:" $cert.Thumbprint