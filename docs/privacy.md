# Privacidad y limpieza: alcance del caso

Antes del launcher se revisaron varios componentes publicitarios y de analítica. En el TV del caso se modificaron siete registros revisados, y tres servicios seguían inactivos después de un reinicio completo. El usuario comprobó sus apps, mando y sonido tras ese primer grupo. Es una observación de ese equipo, no una lista que deba aplicarse igual en otros televisores.

La precarga de cuatro componentes Daily+/Arte se redujo, pero procesos de Arte volvieron a aparecer. No se atribuye a ese cambio una mejora general de rendimiento. El consentimiento Viewing seguía en true y otros componentes de reconocimiento/análisis permanecían disponibles; no se auditó todo el tráfico cifrado ni se promete eliminar toda analítica.

El launcher oculta componentes elegidos de su propia lista. Eso no los desinstala ni garantiza que no se ejecuten en segundo plano. El Inicio de Samsung sigue disponible desde la casa superior y puede mostrar sus contenidos nativos.

Para otro modelo, inventariá servicios, dependencias, condiciones y los campos actuales antes de decidir un cambio. Cada grupo necesita su inverso y una validación de las funciones que quiera conservar ese dueño. No hay una receta masiva de «debloat» en este repositorio. No se deshabilitan imagen, sonido, HDMI, accesibilidad, red, actualizaciones ni administración de apps como una supuesta mejora genérica.

El ensayo del cliente publicitario no mostró una mejora consistente de fluidez; ver [rendimiento](performance.md). El ajuste persistente del teclado conservó el ejecutable y usó una copia de recursos con dos rutas distintas, según [teclado y voz](keyboard-and-voice.md). Los binarios y recursos de Samsung no se redistribuyen.

Los resultados de `run_tv.py`, el catálogo instalado, los nombres de entradas, las preferencias y `/tmp/quiet-home-state.json` pueden revelar datos de uso o del dispositivo. Guardalos en `.local/` y compartí solo observaciones necesarias, sin identificadores ni logs crudos. Las rutas de sistema, IDs de paquetes, modelo, firmware y hashes de binarios stock que figuran en la guía son referencias técnicas para reproducir las comprobaciones.
