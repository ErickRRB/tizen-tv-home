# Adaptar el trabajo a otro Samsung Tizen

El punto de partida es el QN75QN70FAGCZB con firmware 1301.0. En ese equipo se pudo ejecutar un launcher .NET/NUI dentro de un contenedor Samsung existente y dirigir el botón Home hacia él. Para otro TV, reutilizá el orden de trabajo y las comprobaciones; cada diferencia puede requerir un cambio de código o una ruta de acceso distinta.

## 1. Separá las funciones que buscás

Empezá por un launcher que abra apps y HDMI y permita volver a Samsung Home. Dejá para después el cambio del botón Home y los ajustes opcionales de tienda, teclado, voz o servicios. Así podés probar cada parte y volver atrás sin mezclar sus efectos.

Decidí qué apps, entradas, controles de voz, funciones de audio y opciones de accesibilidad necesitás conservar. La selección del caso de estudio no es una lista de componentes que otro dueño deba deshabilitar.

## 2. Hacé un inventario propio

Copiá [la plantilla](../examples/device-inventory.template.json) a `.local/device-inventory.json` y completala con lecturas de tu TV. Los `null` indican datos que faltan. No es un perfil ejecutable para el instalador.

| Dato | Cómo obtenerlo | Para qué sirve |
|---|---|---|
| Modelo y firmware | Ajustes → Soporte → Acerca de este TV | Elegir el caso de referencia y detectar una actualización. No publiques serie, MAC ni QR. |
| Arquitectura y runtime | `uname -m` y enlace de `/usr/share/dotnet.tizen/netcoreapp` | Elegir bibliotecas, ABI y versión de .NET compatibles. |
| Acceso disponible | SDB y una comprobación de la ruta root compatible | Separar Developer Mode de la capacidad real de leer o cambiar archivos de sistema. |
| Contenedor y originales | Paquete instalado, DLL de entrada, runtimeconfig, propietario, permisos, SMACK y hashes | Saber qué se sustituiría y cómo restaurarlo. |
| Esquema e inventario de apps | `.schema` antes de consultar tablas concretas | Adaptar las consultas y elegir IDs de apps que existen en ese equipo. |
| Home y arranque | Probe y preferencias `home_appid` / `startup_appid` | Capturar los tres destinos antes de modificar uno. |
| Entradas y API nativas | `get_source_list` mediante Probe y revisión de bibliotecas | No adivinar qué entero corresponde a cada HDMI. |

[read-only.json](../examples/read-only.json) muestra lecturas iniciales del caso probado. Revisá las rutas antes de ejecutarlo. Si una tabla o biblioteca no existe, registrá esa diferencia y adaptá la lectura; un resultado parcial no acredita compatibilidad.

## 3. Compará con el caso probado

| Aspecto | Observación en QN70F | Qué revisar en otro equipo |
|---|---|---|
| Plataforma | Tizen 9, ARMv7, .NET 6 | Año y versión de Tizen no garantizan la misma ABI ni las mismas API. |
| App reutilizada | `com.samsung.tv.bixby-promotion`, `BixbyPromotion.dll` | Existencia, permisos de plataforma, dependencias y coste de reemplazar esa función. |
| Datos de la app | Rutas bajo `apps_rw/com.samsung.tv.bixby-promotion` | Directorio escribible por el proceso y permisos reales. `owner` es un nombre de ruta del sistema. |
| Usuario de la app | UID 5001 | Identificar el UID de la aplicación; no confundirlo con el de la sesión root. |
| Entradas | TV 0, HDMI 1–4 con IDs 13–16 | Leer fuentes y comprobar cada puerto con imagen y sonido. |
| Home original | `com.samsung.tv.csfs` | Consultar el destino actual; no asumir el mismo ID. |
| Arranque original | `org.tizen.tv-viewer` | Preservarlo si solo querés cambiar Home. |
| Mejoras opcionales | Boost, hint de ventana y marca de guía de volumen | Referencias, símbolos, claves y permisos presentes en ese firmware. |

Un hash de un binario original identifica una versión del archivo, no un TV ni una cuenta. Los hashes del perfil público sirven para rechazar archivos distintos; comparalos con tus originales y guardá sus copias fuera de Git.

## 4. Localizá los supuestos en el código

El JSON de perfil documenta el caso. **No configura por sí solo todas las rutas del launcher.** Estos son los puntos que requieren revisión al portar:

| Archivo / símbolo | Supuesto que hay que revisar |
|---|---|
| `launcher/src/Home.cs`: `Root`, `Self`, `CatalogCache`, `PrefFile` | Contenedor, recursos y almacenamiento de preferencias. |
| `RefreshSources`, `SourceCard` y `DllImport` | IDs de fuentes, cálculo de puertos, formato JSON y nombres/firmas de bibliotecas nativas. |
| `Build`, `OnAppControlReceived`, `Activate`, `SendLaunch` | IDs de Samsung Home, ajustes y tienda; parámetros de búsqueda. |
| `DefaultHidden`, catálogo y `OrderApps` | Qué apps se ocultan o aparecen primero; adaptalo a las preferencias del dueño. |
| `RequestStartupBoost`, `UpdateVolumeGuide` y hints de ventana | Funciones privadas opcionales. Un `catch` no resuelve una referencia ausente al compilar. |
| `launcher/src/Home.csproj` | Versión de runtime, nombre de assembly y referencias Tizen del equipo. |
| `launcher/probe/Program.cs` | API para fuentes, Home, volumen y mute. Revisar ABI antes de ejecutarlo. |
| `tools/build.ps1`, `tools/make_preview_plan.py` | Nombres de artefactos, contenedor, UID, SMACK, hashes, rutas y restauración. |
| `tools/inventory-commands.json` | Tablas, columnas y selección de idioma del catálogo. |
| `tools/store-back-patch` | Parche separado, limitado a un original concreto; no es portable cambiando su hash. |

El generador de vista previa rechaza otros modelos y firmwares a propósito. Para ampliar el soporte, prepará otro perfil y revisá los supuestos anteriores, las operaciones inversas y sus tests. No cambies el modelo declarado ni elimines las guardas para hacer que acepte un TV distinto. Si falta el contenedor o una ruta root compatible, este método todavía no está adaptado a ese equipo.

## 5. Probá sin reasignar Home

Seguí la [instalación de prueba](installation.md): compilá con referencias compatibles, guardá originales con sus metadatos y prepará la inversa. Mantené Samsung Home como destino del botón mientras abrís manualmente la aplicación modificada.

Comprobá con el mando navegación, selección, apertura de apps, regreso, ajustes y cada HDMI con imagen y sonido. Verificá la versión que realmente ejecuta el proceso. En el caso probado, terminar y abrir la app no siempre descargó la DLL anterior; un reinicio normal con la sesión root cerrada permitió validar la nueva.

Cuando la prueba manual funcione, [asigná Home](home-button.md). Revisá por separado un arranque completo, standby y regreso desde una app. Si cambió el firmware durante el trabajo, repetí la comparación antes de reutilizar un plan anterior.

## 6. Registrá qué comprobaste

Distinguí estos estados en tus notas: **inventario realizado**, **acceso comprobado**, **compilación lograda**, **prueba manual en TV**, **Home verificado**, **reinicio/standby verificados** y **restauración verificada**. Uno no implica el siguiente.

Para compartir otro modelo, publicá modelo/firmware, diferencias de API o rutas, cambios de código y resultados de esas pruebas. Conservá fuera del repositorio los originales, catálogos personales, direcciones, cuentas, logs y capturas con identificadores. Documentá cualquier función que no hayas probado.
