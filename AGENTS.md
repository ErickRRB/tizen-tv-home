# Trabajar con esta guía

Leé README.md, docs/adapting-other-tvs.md y docs/recovery.md antes de proponer cambios en un TV. El caso de referencia es QN75QN70FAGCZB / T-PTMFUABC-0090-1301.0. El objetivo es ayudar a adaptar el método con evidencia de cada equipo.

- Usá las preferencias y la autorización vigente del dueño. Obtené modelo, firmware, IP explícita y funciones que quiere conservar cuando falten; no escanees la red ni reutilices datos de otro TV.
- Inventariá antes de escribir. Developer Mode, root, compilación y compatibilidad del launcher son comprobaciones distintas. No falsifiques el modelo/firmware ni elimines guardas para pasar una validación.
- Prepará originales, hashes, metadatos e inversa antes de cada cambio. Conservá la primera copia y prepará restauraciones por archivo o campo; no sobrescribas registros vivos con una base completa antigua.
- Instalá y verificá manualmente antes de asignar Home. Preservá startup si no se pidió cambiarlo. Probá reinicio, standby, mando, apps, HDMI, sonido y recuperación por separado.
- Ejecutá las sesiones SDB/root de forma secuencial. Cerrá la sesión y verificá su limpieza antes de reiniciar. Un fallo puede dejar operaciones previas aplicadas: inspeccioná antes de repetir.
- Usá `.local/` para referencias Samsung, catálogos reales, planes, logs, fotos, builds y respaldos. Publicá solo fuente, recursos redistribuibles y observaciones sin datos personales. Revisá el diff y los archivos preparados para commit.
- Conservá atribuciones y licencias. No distribuyas firmware ni DLL de Samsung. No modifiques bootloader, claves SDB, passwd ni servicios ajenos para hacer funcionar este launcher.

Al cambiar un generador, probá las guardas y las operaciones de restauración con datos sintéticos. Compilá las modificaciones NUI contra referencias compatibles cuando estén disponibles; distinguí esa compilación de una prueba física. No presentes mediciones de foco como tiempo hasta vídeo ni afirmes mejoras de FPS o privacidad que no se midieron.
