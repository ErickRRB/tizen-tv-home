# Compatibilidad y acceso

Si tu modelo o firmware difiere, empezá por [adaptar a otros televisores](adapting-other-tvs.md). Los comandos y rutas de esta página describen el caso de referencia.

El perfil probado es QN75QN70FAGCZB, Tizen 9, ARMv7/32 bits, .NET 6 y firmware T-PTMFUABC-0090-1301.0. La resolución física es 3840 × 2160; la escena NUI de este launcher mide 1920 × 1080. El perfil JSON es documentación de ese caso, no una autorización para modificar todos los televisores con un nombre parecido.

En un TV nuevo, anotá el modelo y firmware desde Acerca de este TV. No publiques serie, MAC, cuentas ni códigos QR. Comprobá runtime, arquitectura, disponibilidad del contenedor com.samsung.tv.bixby-promotion y las referencias de Tizen. Obtené los IDs de entradas desde get_source_list; 13–16 para HDMI y 0 para TV son observaciones del modelo probado. La biblioteca libsource-api.so y las API de WAS/AppControl pueden variar.

## Preparar conexión

Instalá SDB desde Tizen Studio y seguí la documentación de Samsung: [conectar un TV para desarrollo](https://developer.samsung.com/smarttv/develop/getting-started/using-sdk/tv-device.html). En el TV probado, Apps → configuración → 12345 permite activar Developer Mode y registrar la IP de la PC. Reiniciá el TV. Usá su IP explícita; no hace falta escanear la red.

El launcher de este proyecto reemplaza una aplicación de sistema existente. Ese método necesita acceso root compatible; Developer Mode por sí solo no da permiso para sobrescribirla. El caso documentado usó archive-root de [samsung-tv-root 0.0.12](https://github.com/chris-ritsen/samsung-tv-root). No se distribuyen aquí ese ejecutable, payloads ni SDK. Leé las instrucciones y compatibilidad de la versión que obtengas. No sustituyas otro exploit de kernel por conjetura.

`tools/run_tv.py` adapta el transporte usado en este trabajo y exige rutas explícitas a una distribución 0.0.12 obtenida por separado. Es una herramienta avanzada, no un verificador universal de compatibilidad. Instalá sus dependencias según el proyecto original. Comprobá modelo/firmware manualmente antes de usarla; el helper no puede certificar todo el hardware ni las API privadas.

```powershell
python tools/run_tv.py --help
```

Los comandos se proporcionan como un array JSON. Creá `.local/`, copiá [read-only.json](../examples/read-only.json) como `.local/read-only.json` y revisá las rutas. Empezá solo con `id` y lecturas. Reemplazá los marcadores de la línea siguiente con las rutas y direcciones de tu propio equipo:

```text
python tools/run_tv.py --release RUTA_RELEASE --sdb RUTA_SDB --tv-ip IP_TV --pc-ip IP_PC --commands .local/read-only.json
```

El resultado debe mostrar UID/EUID 0, autenticación y limpieza exitosa. Los resultados completos se guardan bajo `.local/sessions`, ignorado por Git. Cada comando tiene un límite de 65.536 bytes. La API puede ejecutar comandos posteriores aunque uno anterior falle: agrupá operaciones dependientes en un solo `set -eu` y no pases a la siguiente fase tras cualquier FAIL. No reinicies ni pongas el TV en standby mientras la sesión root siga abierta. No ejecutes dos herramientas contra el TV al mismo tiempo.

Si root no está disponible o el contenedor no existe, este procedimiento no aplica. Un dispositivo externo con su propio launcher es una alternativa; no se ha probado instalar este NUI como una app comercial firmada normal.

La compilación V24 requiere también una referencia privada Tizen.TV.System.Boost.dll del mismo TV/SDK compatible. No se distribuye. La API privada y el permiso de plataforma de un contenedor Samsung pueden no existir en otro equipo. El código conserva la verificación del sistema y no otorga permisos. No sustituir esta biblioteca por una de otra versión para eludir la comprobación.

La marca de ayuda de volumen de V27 requiere también Tizen.TV.System.dll del equipo compatible. Se usa Vconf.GetBool/SetBool a través del wrapper original; si la clave o el permiso no están disponibles, el launcher continúa funcionando y registra el fallo. No se modifica ningún nivel de volumen, mute ni política de audio.
