# Loquendo TTS

Programa de texto a voz para Windows (WinForms, C#) con voces "estilo Loquendo".
No incluye ni usa el motor ni las voces originales de Loquendo; son aproximaciones
hechas con motores libres y las voces de Windows.

## Motores de voz
- **Piper** (voces neuronales offline, español de España y México)
- **eSpeak NG** (voz robótica clásica)
- **Voces de Windows** (SAPI)

Incluye presets (Jorge, Diego, Carlos, Soledad, Francisca, Esperanza) que ajustan
velocidad y tono de una voz base. Sin límite de caracteres: el texto se lee por trozos.

## Uso
1. Ejecuta `setup.ps1` (descarga Piper, eSpeak NG y las voces; ~300 MB).
2. Ejecuta `build.ps1` para compilar `LoquendoTTS.exe` (usa el compilador de .NET Framework incluido en Windows).
3. Abre `LoquendoTTS.exe`.

```powershell
powershell -ExecutionPolicy Bypass -File setup.ps1
powershell -ExecutionPolicy Bypass -File build.ps1
```

## Licencias de terceros
Piper (MIT), eSpeak NG (GPLv3) y cada modelo de voz tienen sus propias licencias.
Revisa las de los modelos en https://huggingface.co/rhasspy/piper-voices antes de redistribuirlos.
