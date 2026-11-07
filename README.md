# Sistema de Seguimiento de Solicitudes

MANTEN TU VISUAL STUDIO ACTUALIZADO PARA EVITAR PROBLEMAS CON EL FRAMEWORK

Revisa la conexión en el appsettings.json en el proyecto "SolicitudesAPI", solo cambia Server=(nombre de tu servidor).

LA BASE DE DATOS CORRECTA ES LA QUE SE LLAMA "SistemaSolicitudes.bacpac", ignorar las demás al migrar el sistema a otro equipo.

La base de datos la pasé aca es el "SistemaSolicitudes.bacpac", en tu sql server management dale clic derecho a "Databases" luego le das a "import Data-tier application", procura que la base de datos se llame SistemaSolicitudes

Para abrir el sistema es en cualquier navegador en: localhost:7123

Si te lanza errores de seguridad para abrir el sistema en visual studio asegúrate de que al descargar el .rar del sistema este desmarcada la opción de "Solo lectura" y este desbloqueado el contenido, esto lo puedes averiguar dándole clic derecho sobre el archivo y en propiedades.

## Estilos con Tailwind

Los estilos compartidos y la paleta institucional están en `Sistema de Seguimiento de Solicitudes/wwwroot/css/tailwind-input.css` y `tailwind.config.js`. Para generar el CSS antes de ejecutar el sistema:

```powershell
npm --prefix "Sistema de Seguimiento de Solicitudes" install
npm --prefix "Sistema de Seguimiento de Solicitudes" run build:css
```

Durante el desarrollo, `npm --prefix "Sistema de Seguimiento de Solicitudes" run watch:css` vuelve a generar los estilos al guardar cambios. Para las vistas nuevas, usa utilidades Tailwind y la paleta `brand`; evita crear hojas `.razor.css` por página.
