# Despliegue de produccion

Imperio Digital se despliega con el frontend Vite en Vercel, el backend ASP.NET Core en Render y PostgreSQL en Supabase.

## Render

1. Conecta el repositorio a Render mediante el Blueprint `render.yaml`.
2. Completa los valores marcados como secretos en el servicio:
   - `ConnectionStrings__DefaultConnection`
   - `Jwt__Key`
   - `OpenRouter__ApiKey`
   - `OpenRouter__SiteUrl`
   - `Frontend__Url`
3. Usa la cadena de conexion de Supabase en `ConnectionStrings__DefaultConnection`.
4. Cuando Render asigne la URL publica, configura `OpenRouter__SiteUrl` con esa URL HTTPS.
5. Verifica `https://<render-service>/health`. Swagger queda disponible solo en desarrollo.

El contenedor escucha automaticamente el valor de `PORT` de Render. No se ejecutan migraciones durante el despliegue porque esta entrega no cambia el modelo persistido.

## Vercel

1. Importa el mismo repositorio y configura `Frontend` como Root Directory.
2. Usa `npm run build` como Build Command y `dist` como Output Directory.
3. Define estas variables de produccion antes de desplegar:
   - `VITE_API_URL=https://<render-service>/api`
   - `VITE_METHODOLOGY_EXPERIENCE_V2=true`
4. Copia la URL HTTPS final de Vercel en `Frontend__Url` del servicio Render y redepliega el backend.

Las variables `VITE_*` son publicas y se incorporan en el build del navegador. No almacenes secretos del backend en Vercel.

## Verificacion posterior

1. Abre `/health` en Render.
2. Inicia sesion como Docente y Estudiante.
3. Crea, publica y asigna un escenario a un curso.
4. Ejecuta una simulacion completa y revisa resultados.
5. Confirma que los paneles se actualizan mediante SignalR.

## Seguridad operativa

Antes del primer despliegue, rota las credenciales de Supabase y OpenRouter usadas durante desarrollo. Los secretos solo deben existir en variables de entorno o archivos locales ignorados por Git.
