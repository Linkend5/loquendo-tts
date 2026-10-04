$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Set-Location $PSScriptRoot
$dl = Join-Path $PSScriptRoot '_dl'
New-Item -ItemType Directory -Force $dl | Out-Null

# Piper
Invoke-WebRequest "https://github.com/rhasspy/piper/releases/download/2023.11.14-2/piper_windows_amd64.zip" -OutFile "$dl\piper.zip"
Expand-Archive "$dl\piper.zip" $PSScriptRoot -Force

# eSpeak NG (se extrae sin instalar)
Invoke-WebRequest "https://github.com/espeak-ng/espeak-ng/releases/download/1.52.0/espeak-ng.msi" -OutFile "$dl\espeak-ng.msi"
$tmp = Join-Path $PSScriptRoot 'espeak_tmp'
Start-Process msiexec -ArgumentList "/a `"$dl\espeak-ng.msi`" /qn TARGETDIR=`"$tmp`"" -Wait
if (Test-Path "$PSScriptRoot\espeak") { Remove-Item "$PSScriptRoot\espeak" -Recurse -Force }
Move-Item "$tmp\eSpeak NG" "$PSScriptRoot\espeak"
Remove-Item $tmp -Recurse -Force

# Voces de Piper
New-Item -ItemType Directory -Force "$PSScriptRoot\voces" | Out-Null
foreach ($p in 'es/es_MX/ald/medium', 'es/es_MX/claude/high', 'es/es_ES/davefx/medium', 'es/es_ES/sharvard/medium') {
    $files = Invoke-RestMethod "https://huggingface.co/api/models/rhasspy/piper-voices/tree/main/$p" | Where-Object { $_.path -match '\.onnx(\.json)?$' }
    foreach ($f in $files) {
        Invoke-WebRequest "https://huggingface.co/rhasspy/piper-voices/resolve/main/$($f.path)" -OutFile (Join-Path "$PSScriptRoot\voces" (Split-Path $f.path -Leaf))
    }
}
Remove-Item $dl -Recurse -Force
Write-Host "Listo."
