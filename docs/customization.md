# Apps, entradas y fondo

Las apps iniciales se seleccionan en el catálogo privado. Mantener OK abre mover, cambiar nombre y ocultar; esos cambios afectan al launcher. SmartThings, Security Center, Vision AI Companion y las apps de ayuda indicadas en DefaultHidden se ocultan aquí, sin desinstalarlas ni detener servicios.

El catálogo se consulta al regresar y cuando termina la entrada diferida. Una app nueva debería aparecer si no está en BaselineIds o en la lista de ocultas. Las apps actuales se conservan; el launcher no borra sus datos ni credenciales.

Las entradas usan get_source_list y notificaciones nativas. El perfil probado usa 13, 14, 15 y 16 para HDMI 1–4 y 0 para TV. Nuevos dispositivos actualizan la lista cuando el TV emite el evento o al regresar. Con más entradas que posiciones visibles, la ventana se desplaza; al pasar la última vuelve a la primera. Para otro modelo revisá el filtrado y el cálculo de puertos en Home.cs.

## Fondo sin estirar

Colocá tu imagen local en `.local/assets/background.jpg` antes de preparar el stage. Si falta, el paquete público usa background-default.svg.

La foto se dibuja con ScaleToFill, que en esta biblioteca NUI corresponde a OverFitKeepAspectRatio: ocupa 16:9 conservando proporciones y recortando el sobrante. Una foto de 2048 × 1365 queda aproximadamente como un recorte central de 2048 × 1152. La pantalla física 4K no vuelve 4K nativa una imagen de menor tamaño. La escena del launcher es 1920 × 1080 y solicita decodificar para ese tamaño.

La capa negra al 28 % mejora el contraste de los nombres blancos. No se genera otra fotografía ni se alteran las proporciones del sujeto.

## Logos opcionales

El comportamiento normal usa el icono de la app instalada. Podés aportar localmente app-mercado-play-centered.png o app-crunchyroll-vector.svg para mejorar esos dos iconos, siempre con derechos para el recurso que uses. Si no están presentes, el código público vuelve al icono instalado. No se incluyen las imágenes de marca usadas en el TV del caso de estudio.
