# Abrir el launcher con el botón Home

Primero comprobá que el acceso Bixby ya abre nuestro launcher, que funcionan las apps y que la casa superior abre Samsung. La casa usa el app_id com.samsung.tv.csfs en el perfil probado; revisalo antes de adaptar el código a otro modelo.

Compilá launcher/probe/HomeProbe.csproj y colocá HomeProbe.dll y su runtimeconfig en TU carpeta de respaldo del TV. Es código propio: consulta la operación main y las fuentes; el modo `set-home` modifica solo esa operación. No contiene binarios Samsung.

Estos son comandos para la sesión del TV, no para PowerShell. Sustituí RUTA_PROBE por la ruta real del probe:

```sh
/usr/bin/dotnet RUTA_PROBE
vconftool get db/appfw/home_appid
vconftool get db/appfw/startup_appid
```

Guardá los tres valores originales antes de cambiarlos. En este equipo, main y home_appid eran com.samsung.tv.csfs, pero startup era org.tizen.tv-viewer. No copies esos valores como originales de otro TV.

Después de preparar una restauración que use TUS valores y de comprobar que el hash del launcher coincide con TU build probado:

```sh
/usr/bin/dotnet RUTA_PROBE set-home com.samsung.tv.bixby-promotion
vconftool set -f -t string db/appfw/home_appid com.samsung.tv.bixby-promotion
```

Leé ambos selectores otra vez y verificá que cambiaron. Esta configuración conserva startup_appid: no hace falta cambiar el arranque para reasignar Home. No modifiques startup por copiar una receta que lo iguale al launcher.

Mantené la restauración en el TV y en la PC: volver a ejecutar set-home con la operación original y restaurar home_appid con su valor original, comprobando primero que todavía apuntan a nuestro launcher. Si la segunda escritura falla después de cambiar la primera, revertí la primera antes de seguir.

Terminá la sesión root y verificá su limpieza. Probá con el mando físico Home desde un HDMI y desde una app, la casa superior hacia Samsung, volver a Home, standby/wake y un arranque completo. Una tecla simulada desde la PC no garantiza la misma ruta del mando o del teclado virtual. En el equipo documentado el usuario confirmó Home físico hacia nuestro launcher; el arranque automático se dejó en TV.

## Entrada más directa

El código consulta los hints que anuncia la ventana. Solo si el TV anuncia wm.policy.win.effect.disable solicita valor 1 para esta ventana. No cambia duraciones globales ni efectos de otras apps. La aceptación del hint no prueba por sí sola una tasa de cuadros concreta: hay que revisar el resultado visible.
