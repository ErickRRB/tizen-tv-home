# Regreso desde Buscar apps

El launcher abre la tienda nativa con AppControl y Sub_Menu=search. La búsqueda, instalación y teclado siguen perteneciendo a Samsung.

En el firmware del caso de estudio, Atrás desde búsqueda terminaba en el catálogo promocional. Se corrigió el cierre de SearchAllView.HideSearchHandler de CommonStore.dll: para la búsqueda de entrada directa se oculta el panel, se solicita ocultar la tienda y se vuelve al caller si existe; dentro de rutas anidadas se conserva Back del router.

El generador opcional en tools/store-back-patch trabaja sobre una copia LOCAL del original y rechaza cualquier SHA distinto del original revisado. Conserva el archivo PE, metadatos, MVID y todos los bytes fuera del cuerpo del método. No incluye CommonStore.dll ni su imagen nativa. Para otro firmware, ese generador debe rechazar la entrada; requiere una revisión distinta, no quitar el guard.

La sustitución en el TV requiere respaldo privado, detener la tienda, preservar metadata y apartar su caché nativa original; no está incluida en el instalador del launcher. Su inverso debe restaurar DLL y caché originales después de comprobar los hashes actuales. No reemplaces una DLL en uso.

El usuario confirmó con el mando físico que Atrás vuelve al launcher. Una simulación WM no recorrió la misma ruta del IME y no se usó como prueba de esa tecla. Las primeras variantes locales del parche tenían un cuerpo IL inválido por terminar el relleno con NOP; la versión publicada conserva un ret terminal y fue verificada por ejecución de las ramas y en el TV. No se distribuyen esas variantes fallidas.

## Generar y verificar localmente

```powershell
dotnet run --project tools/store-back-patch/StoreBackPatch.csproj -- .local/original/CommonStore.dll .local/patched/CommonStore.dll
dotnet run --project tests/store-back-behavior/Tests.csproj -- .local/patched/CommonStore.dll
```

El primer comando no permite sobrescribir la salida. Ninguno instala el archivo en el TV. La segunda prueba ejecuta las ramas IL reales con dependencias sustituidas para verificar búsqueda directa/interna y caller vacío/presente; la comprobación con mando físico sigue siendo necesaria.
