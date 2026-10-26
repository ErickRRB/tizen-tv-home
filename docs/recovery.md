# Restauración

No borres primero los respaldos ni hagas un restablecimiento de fábrica esperando recuperar archivos de sistema. El reset puede borrar datos de usuario y dejar intacta una sustitución del paquete. Conservá Developer Mode y el acceso desde la PC hasta comprobar la recuperación.

Orden de restauración:

1. Leer los selectores y exigir que sigan apuntando a nuestro launcher. Restaurar operación main y home_appid a los valores capturados en ESE TV. Preservar startup si este proyecto no lo cambió.
2. Detener solo com.samsung.tv.bixby-promotion, usando el UID observado. Esperar que realmente deje de ejecutarse.
3. Exigir el hash exacto de nuestro DLL y de los originales guardados. Copiar los originales con sus metadatos; sustituir mediante temporales y renombrado. Restaurar también el runtimeconfig si se cambió.
4. Devolver la caché nativa original que se había movido. Si el sistema generó una caché nueva, conservarla apartada antes de devolver la original; no mezclar ambas.
5. Mover nuestros recursos a una carpeta de recuperación. No borrar datos de streaming ni restaurar SQLite completos. Si personalizaste títulos/iconos, invertir solo tus campos y recursos registrados.
6. Abrir el Home original, cerrar root con limpieza verificada y probar mando, ajustes, sonido, HDMI, standby y arranque.

Un guard que falla indica que el estado cambió, por ejemplo tras una actualización del TV. No lo elimines para forzar una copia antigua. Compará el estado nuevo con el respaldo privado y prepará un inverso nuevo. El primer respaldo original se conserva; las actualizaciones de nuestro código necesitan solo su fuente y un inverso que apunte al build anterior, no otro snapshot completo del TV.

Los cambios de servicios o bibliotecas publicitarias tienen su propia restauración y no se revierten quitando el launcher. Los ejemplos de rendimiento no son una lista de paquetes para deshabilitar indiscriminadamente.

En el equipo probado, un proceso precargado siguió usando la DLL anterior mapeada incluso después de terminar y abrir la app. El cambio de hash en disco no garantiza que el código nuevo se esté ejecutando. Verificá versión y estado del proceso nuevo; si la inversa restaura el archivo pero persiste código precargado, coordiná un reinicio normal y verificá el identificador de arranque. No reinicies loaders globales ni uses un reinicio forzado para resolverlo.

La vista previa registra también la marca transitoria de ayuda de volumen cuando existe y tiene tipo booleano. Si era false y el launcher la dejó en true, la inversa la repone en false; si el valor o tipo ya no coinciden con ese caso, no adivina un reemplazo. No modifica volumen ni mute. La caché del proceso puede requerir el reinicio normal indicado arriba para terminar de ejecutar el código anterior.
