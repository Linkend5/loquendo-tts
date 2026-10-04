$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$ref = (Get-ChildItem C:\Windows\Microsoft.NET\assembly\GAC_MSIL\System.Speech -Recurse -Filter System.Speech.dll | Select-Object -First 1).FullName
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /out:LoquendoTTS.exe "/r:$ref" /r:System.Windows.Forms.dll /r:System.Drawing.dll LoquendoTTS.cs
if ($LASTEXITCODE -ne 0) { throw "Fallo la compilacion" }
Write-Host "Listo: LoquendoTTS.exe"
