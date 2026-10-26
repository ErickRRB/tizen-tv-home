# Compilar y probar antes de cambiar Home

Estos pasos documentan el método usado y sus verificaciones. Para otro modelo o firmware, empezá por [adaptar a otros televisores](adapting-other-tvs.md). La secuencia de instalación completa en un TV de fábrica requiere una revisión del perfil de ese aparato. No ejecutes el perfil de otro dueño ni copies sus bases de datos.

## Inventario privado

Con una conexión root ya comprobada, revisá primero los esquemas con [las lecturas iniciales](../examples/read-only.json). Después adaptá y ejecutá [el inventario de apps](../tools/inventory-commands.json): consulta las tablas del firmware probado y exporta CSV de aplicaciones y sus etiquetas. Si faltan tablas o columnas, detenete y adaptá la consulta, sin escribir sobre el registro. Guardá los stdout de las dos primeras consultas del inventario como `.local/apps.csv` y `.local/baseline.csv`. Podés extraerlos del JSON de sesión con un script local; los encabezados son `app_id,app_label,app_icon` y `app_id` respectivamente.

Elegí los IDs que quieras mostrar inicialmente. Ejemplo para el firmware probado, verificando antes que esos IDs existen en TU exportación:

```powershell
python tools/make_catalog.py --apps .local/apps.csv --baseline .local/baseline.csv --include org.tizen.netflix-app --include 9Ur5IzDKqV.TizenYouTube --output .local/catalog.json
```

BaselineIds es la lista de apps que ya existían al preparar el TV. Permite ocultar las preinstaladas no elegidas y mostrar las apps nuevas que instales después. No uses el ejemplo de catálogo con BaselineIds vacío como configuración de producción: mostraría componentes preinstalados que querías ocultar. Los IDs regionales de YouTube, Crunchyroll y otros servicios pueden ser distintos.

## Compilación

Usá un SDK .NET que pueda compilar net6.0. Obtené Tizen.NUI.dll, Tizen.Applications.Common.dll, Tizen.System.dll, Tizen.TV.System.dll, Tizen.TV.System.Boost.dll y Tizen.dll de un SDK autorizado o de TU dispositivo compatible. No los subas al repositorio. Guardalos en `.local/framework`. Las referencias son Private=false para que no entren al paquete.

```powershell
./tools/build.ps1 -FrameworkDir .local/framework
python -m unittest discover -s tests -v
```

El resultado queda en `.local/stage`. Los datos privados, binarios y catálogos están ignorados por Git. Para repetir una compilación de paquete, renombrá el stage anterior y conservá su hash; el script no lo borra.

## Generar el plan para revisar

El generador opcional solo acepta el modelo/firmware del perfil revisado y sus hashes originales. No se conecta al TV. En otro modelo debe rechazar el perfil: no cambies el nombre del TV para eludirlo.

```powershell
python tools/make_preview_plan.py --profile profiles/qn75qn70fagczb-1301.json --stage .local/stage --name preview-01 --output .local/plans/preview-01
```

Revisá apply-preview.sh, restore-preview.sh y manifest.json, incluidos los hashes, paths, UID y etiqueta SMACK. El flujo completo generado no se ensayó otra vez en un segundo TV de fábrica; documenta y automatiza la secuencia revisada del caso original. Hace falta validar el perfil fresco y conservar los originales en la PC.

Ejecutá primero stage.json mediante tools/run_tv.py. Comprueba cada SHA-256; si termina con FAIL, no apliques nada ni borres el directorio para ocultar el fallo. Solo tras revisar un resultado limpio ejecutá apply-preview.json en otra sesión. Ese paso toma una copia inicial nueva y exclusiva de los archivos que va a cambiar, prepara su inverso, aparta la caché nativa y pide abrir la vista previa. No cambia Home ni startup. Conservá sus resultados privados y copiá también el respaldo original a la PC por un transporte verificado.

Para recuperar esa primera vista previa, restaurá Home antes si lo habías cambiado y ejecutá restore-preview.json. El inverso exige nuestros hashes actuales y los originales, y conserva los recursos apartados en vez de borrarlos. Estos planes no sirven como actualizador sobre un launcher ya personalizado: ese caso necesita un inverso que apunte al build anterior, sin duplicar el primer respaldo completo.

## Copia original e inverso

Antes de reemplazar nada, registrá privadamente:

- Hash, propietario, permisos y etiqueta SMACK del DLL y runtimeconfig originales.
- Presencia de bin/.native_image y sus hashes.
- Operación main actual, db/appfw/home_appid y db/appfw/startup_appid.
- Modelo, firmware, arquitectura, runtime y esquema de los registros consultados.

Guardá una única copia inicial del DLL y runtimeconfig originales en la PC y otra en una carpeta nueva bajo /opt/usr/share/quiet-home-backups. Usá cp -a en el TV para conservar los metadatos y verificá los hashes. Mové la caché nativa original a ese respaldo, en vez de borrarla o seguir ejecutándola después de sustituir el DLL. No sobreescribas el primer respaldo al actualizar nuestro launcher.

Prepará el inverso antes del cambio: debe comprobar el hash de nuestro DLL instalado, detener solo este contenedor, restaurar los dos archivos originales con sus metadatos, devolver la caché nativa y mover los recursos personalizados a una carpeta de recuperación. Home debe volver primero a sus valores originales si ya fue reasignado. No restaures una base completa sobre el registro vivo.

## Sustitución para el perfil probado

El contenedor observado fue /opt/usr/apps/com.samsung.tv.bixby-promotion. El destino principal es bin/BixbyPromotion.dll y el runtimeconfig vecino. Los recursos de este proyecto deben quedar en res/quiet-home, incluido catalog.json y fonts/. Su propietario y etiqueta deben coincidir con los archivos legibles originales del paquete; no asumas UID/GID o etiquetas de otro modelo.

Transferí el stage propio por el transporte autorizado, verificando SHA-256 de cada archivo. Podés fragmentar base64 en bloques de 45056 bytes de entrada para no superar el límite de comando. No continúes si una transferencia está truncada.

Para cada sustitución usá un temporal junto al destino, conservando su metadata con cp -a; escribí los bytes nuevos en ese temporal, verificá su hash y renombralo al destino. Los guards deben exigir los hashes originales recién registrados. No uses `chmod 777` ni desactives SMACK. Prepará en el TV el inverso y su copia original antes de solicitar el lanzamiento.

El acceso de Bixby abre el launcher porque se reutiliza ese contenedor. En el perfil probado estaba habilitado; no lo deshabilites como parte de una limpieza. La guía básica no modifica etiquetas, iconos compartidos ni tablas de paquetes. Eso evita copiar SQL específico de un firmware; renombrar Bixby a Inicio es una personalización separada que requiere capturar e invertir solo los campos afectados.

Solicitá la apertura con `app_launcher -u UID_DEL_USUARIO -s com.samsung.tv.bixby-promotion`, usando el UID verificado en ese TV. En el caso documentado era 5001. Primero probá mando, apps, HDMI, imagen, sonido, ajustes y búsqueda. Home sigue en Samsung hasta completar esa revisión. Luego seguí docs/home-button.md.

No reemplaces el ejecutable dotnet, passwd, claves SDB, firmware ni bootloader. No hace falta para el launcher y no se verificó recuperación de esas modificaciones.

Si el hash nuevo está instalado pero se sigue viendo la aplicación anterior, no cambies todavía la ruta de Home. El equipo probado reutilizó procesos con la DLL antigua mapeada; terminar y abrir la app, e incluso direct-start, no bastó. Coordiná un reinicio normal con todas las sesiones root cerradas y verificá después el código/estado nuevo. Un hash en disco por sí solo no valida la vista previa. No uses --force ni reinicies loaders globales para saltarte esta comprobación.
