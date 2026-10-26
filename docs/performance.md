# Rendimiento: cambios y límites

El launcher mantiene la escena al regresar. OnResume y AppControl agrupan la actualización en un timer de 1.000 ms; la señal de cambios de entradas se comprueba cada 500 ms mientras el launcher está activo. Apps y entradas solo reconstruyen la escena si cambia su información. Mover el foco dentro de la misma página o ventana de entradas actualiza el foco sin volver a cargar toda la interfaz.

En el TV del caso de estudio, el contador de reconstrucciones quedó estable en 2 después de la inicialización durante una solicitud repetida, la vuelta desde Samsung Home y el movimiento entre apps/HDMI; se conservaron el catálogo y las fuentes. Eso verifica menos reconstrucciones, no mide FPS ni demuestra que toda transición del sistema sea fluida. El usuario seguía percibiendo un tirón con ese primer cambio; después se añadió el hint de efecto por ventana y el ajuste de carga del fondo.

La aceptación del hint se verificó con ID y lectura del valor 1. La fluidez percibida posterior sigue sujeta a prueba con el mando. No se cambian globalmente animaciones, frecuencia de pantalla, motores de imagen ni servicios de vídeo.

## Ensayo del cliente publicitario: sin mejora consistente

Se identificó un cliente cargado por HomeMedia que intentaba conectarse al puerto 19201 de adagent-service, que estaba inactivo. En un ensayo A/B/A sin uso del mando se probó evitar ese bucle desde una API del cliente publicitario.

Las latencias siguientes miden solicitud hasta foco, no primer cuadro visible; se hicieron tres reaperturas por secuencia.

| Medida observada | Original A | Candidato B | Original restaurado A |
|---|---:|---:|---:|
| Reintentos por segundo |13,14|0,24|11,57|
| Mediana hasta foco de Home |0,96 s|0,90 s|1,08 s|
| Mediana hasta foco de Ajustes |1,29 s|1,52 s|1,38 s|
| CPU observada en la secuencia |58,69 %|68,27 %|41,89 %|

El candidato redujo los reintentos, pero no mejoró de forma consistente los menús y empeoró otras medidas de esa secuencia. Se restauraron los dos archivos originales con guardas de hash, incluida su imagen nativa. El servicio publicitario permaneció inactivo. Ese parche no se distribuye ni se recomienda como optimización.

Son mediciones de un solo equipo, una secuencia concreta y ese firmware; no son un benchmark general ni una garantía para otros modelos. Una primera comparación con intervención simultánea del mando fue descartada y repetida. La documentación conserva el ensayo controlado y el resultado de retirada.

Ocultar Daily+, Arte o una app en el launcher no demuestra que su proceso haya dejado de ejecutarse. Tampoco se promete ausencia total de analítica: no se hizo una auditoría completa del tráfico cifrado.

## Catálogo, selección y envío de aperturas

El catálogo de apps se lee en un trabajador y se liberan los ApplicationInfo. Una caché propia evita una segunda reconstrucción al arrancar; se revalida después de la entrada. La selección ya no escribe un diagnóstico en disco con cada movimiento. Los iconos se amplían al seleccionarlos y usan patrones de sonido del sistema disponibles en el equipo probado.

El envío AppControl se ejecuta fuera del hilo de interfaz y vuelve a este para completar el estado. Se verificaron Ajustes, Buscar apps y reapertura de una app activa. Esto evita bloquear el hilo de interfaz durante la llamada; no acredita menor tiempo de carga interna, menos espera de red ni una tasa de cuadros concreta. La persistencia del teclado y del launcher se verificó tras un reinicio normal; no hay un reaplicador root al arrancar.

V24 solicita además el refuerzo temporal de arranque que utiliza Apps de Samsung, mediante Tizen.TV.System.Boost y su control de permisos original. No altera políticas, gobernadores ni frecuencias manualmente. Una denegación permite continuar con la apertura normal. Se verificó aceptación en el contenedor probado; no equivale a medir una mejora visible ni concede compatibilidad con otros firmwares.

V26 amplía la selección también en entradas y accesos; V27 marca como vista la guía nativa de volumen mediante el wrapper de permisos del sistema. Get/Set/Get se ejecuta una sola vez por proceso en un trabajador; no modifica volumen ni mute. En el primer arranque verificado, antes de pulsar Home, la llamada tardó unos 785 ms en el trabajador y completó en la interfaz. Existe una carrera antes de completarse y no se garantiza ese arranque automático en otros firmwares.

El ensayo muestra por qué conviene separar una decisión sobre actividad publicitaria de una afirmación sobre rendimiento. Los cambios en ese cliente quedan fuera del launcher y necesitan su propio respaldo, revisión e inversa.
