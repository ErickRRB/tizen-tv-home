# Teclado y escucha de voz: procedimiento del equipo probado

Estos ajustes adicionales se verificaron en QN75QN70FAGCZB, Tizen 9 y firmware 1301.0. No los aplica el instalador del launcher. No se distribuyen el teclado, sus recursos originales ni binarios de Samsung. En otro modelo hay que comprobar de nuevo rutas, versiones, permisos y esquema de datos.

## Quitar las variantes al pasar por una letra

El ejecutable original se mantuvo en `/usr/apps/com.samsung.tv.sboard-app/bin/sboard-app` y no se modificó su memoria. El recurso observado fue `res/skb/lang_keys/es_es_abc.json`. Antes de cambiar nada se verificaron los hashes del ejecutable y del JSON, y se guardaron los dos valores originales de rutas del registro de paquetes.

Se creó una copia completa de los recursos bajo almacenamiento modificable de `/opt`, con enlaces a los directorios originales bin/lib/shared. Se editaron únicamente las listas de variantes ABC de ese JSON para conservar la letra base; la tecla independiente ñ y el resto de los recursos se conservaron. En ese equipo se compararon 443 archivos y solo ABC difería. No basta con copiar un JSON suelto: el teclado necesita el árbol de recursos completo.

Una transacción con guardas cambió únicamente `package_info.root_path` y `package_app_info.app_root_path` del paquete y app `com.samsung.tv.sboard-app`. Se conservaron `app_exec`, usuario, datos, caché y todos los registros ajenos. Cada SELECT/UPDATE debía afectar exactamente la fila esperada y coincidir con las rutas y ejecutable originales; cualquier estado distinto obliga a detenerse y preparar un perfil nuevo.

El gestor mantuvo rutas antiguas en caché. La prueba aceptada se hizo después de un reinicio normal, con root ya cerrado. Se verificaron las rutas efectivas, los hashes, los bytes originales del ejecutable en memoria y las capturas del teclado: a no abría el panel y un Down pasaba a h, manteniendo español y ñ. No se instaló un reaplicador de root ni un escritor de memoria al arrancar.

La inversa conserva y restaura solo los dos campos previstos y repone el ABC original también en la copia de recursos. Ese segundo paso evita que una ruta todavía cacheada conserve la modificación después de restaurar el registro. No se restaura toda la base de paquetes ni se elimina el árbol de datos de apps. Un reinicio normal puede ser necesario para comprobar la reversión efectiva. Este procedimiento no es una receta automática para otros teclados o idiomas.

## Desactivar solo la escucha «Hi Bixby»

Primero usá el ajuste normal del TV si está disponible. En el equipo probado se leyó el valor crudo de `db/menu/voice/assistant_farfield` mediante `Tizen.TV.System.Vconf.GetString`, respetando el control de permisos del wrapper original. Ese valor era un objeto JSON con una propiedad `org.tizen.voice-app` igual a `on`.

Se conservó el valor completo anterior, se cambió únicamente esa propiedad a `off` mediante `SetString` y se verificó con otra lectura y comparación del JSON. Un formato o valor desconocido requiere detenerse; no se deben reemplazar otras propiedades o asistentes. La salida humana de `vconftool get` no equivale al JSON crudo y no debe reutilizarse como entrada para este cambio.

La secuencia off/on/off midió aproximadamente 0,01/3,56/0,00 % de CPU total del proceso observado, y off persistió después de reiniciar sin reaplicarlo. Eso acredita la reducción de actividad de ese componente, no una mejora general de FPS o carga de apps. No se detuvo el servicio completo, porque también tiene vínculos con AI Sound. La voz del botón del mando, funciones de audio y sus sensores requieren su propia comprobación física si se quieren conservar.

La inversa debe verificar que el valor actual todavía sea el que se escribió antes de restaurar el valor anterior. Este ajuste no concede privilegios nuevos, no detiene el servicio completo y no modifica el firmware.
