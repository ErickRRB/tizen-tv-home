# Verificación y límites

La versión de dispositivo V27 se verificó en QN75QN70FAGCZB, Tizen 9, firmware 1301.0, ARMv7 y .NET 6. Esta distribución conserva esa implementación con recursos de ejemplo: fondo SVG cuando no hay foto, icono instalado cuando falta arte opcional y selección de fila cuando no hay entradas. El recurso de foto se llama `background.jpg` y el estado de diagnóstico identifica esta distribución como `quiet-home-v27-public`.

## Evidencia del caso de estudio

| Función | Resultado registrado |
|---|---|
| Home y diseño | Botón Home físico y regreso desde búsqueda confirmados. Selectores, catálogo, fuentes y proporciones del fondo persistieron tras reiniciar. |
| Selección | Navegación y capturas de apps, HDMI y accesos; escala centrada de 1,12 en apps/accesos y 1,035 en entradas. |
| Apertura | AppControl en trabajador y continuación en interfaz; solicitudes correctas para ajustes, búsqueda y apps. El refuerzo temporal stock fue aceptado con los permisos del contenedor. |
| Ayuda de volumen | V27 se inició antes de abrir Home manualmente y marcó la ayuda como vista. La operación tardó unos 785 ms fuera del hilo de interfaz. Se confirmó que el volumen cambiaba sin mostrar la guía. |
| Teclado opcional | Se compararon 443 recursos y solo ABC difería. El ejecutable y sus instrucciones en memoria se conservaron. Tras reiniciar, la letra a no abría el panel de variantes y un Down llegaba a h; español y ñ se mantuvieron. |
| Escucha de voz opcional | «Hi Bixby» desactivado persistió sin reaplicarlo. La secuencia off/on/off registró aproximadamente 0,01/3,56/0,00 % de CPU total para el proceso observado, sin detener el servicio completo. |

El reinicio normal cambió el identificador de arranque. No se instalaron servicios root al boot ni reaplicadores de memoria. Las sesiones de validación registraron limpieza del staging y conservación de los archivos de sistema comprobados por el transporte; eso no sustituye revisar cada cambio.

## Comprobaciones del repositorio

Los tests Python comprueban selección y baseline del catálogo, rechazo de IDs inválidos, guardas del plan de primera instalación y restauración de la marca de volumen con un `vconftool` simulado. Ese último caso verifica que un cambio de tipo, un valor desconocido o una lectura fallida no provoquen una escritura.

El launcher y HomeProbe pueden compilarse localmente contra referencias compatibles. Las pruebas del parche de tienda ejecutan las ramas IL de una copia privada; no se incluye esa DLL. Los planes sintéticos y la compilación permiten revisar artefactos, pero no representan una instalación nueva en un TV.

## Qué falta validar en cada adaptación

La instalación completa del repositorio en un segundo TV de fábrica no se ensayó. Tampoco se midieron FPS ni un tiempo fiable hasta contenido reproducible de las apps. Una medición de ajustes por SDB agotó el tiempo de espera y fue descartada.

La última ronda física no cubrió voz con botón del mando, reproducción HDMI, conexión de un HDMI nuevo ni varios ciclos prolongados de standby. Hay que verificar esas funciones en cada equipo. El selector de arranque permaneció en TV; el autoarranque temprano del proceso Home se observó en este firmware y no acredita ese comportamiento en otros.

La publicación con fondo y ejemplos genéricos se verifica localmente. Sus cambios de distribución no se presentan como una nueva prueba física ni amplían el soporte a otros modelos. Usá [la guía de adaptación](adapting-other-tvs.md) para registrar evidencia propia.
