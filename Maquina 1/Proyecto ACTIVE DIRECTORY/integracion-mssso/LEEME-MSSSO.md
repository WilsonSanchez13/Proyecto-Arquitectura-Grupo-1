# Integración del monolito (MSSso, M2) con el Active Directory

1. Copia la carpeta `directorio/` a `src/main/java/com/negociocomercio/mssso/` (si tu paquete base es otro,
   cambia la línea `package` de los 6 archivos y los imports).
2. Agrega a `application.properties`:

       directorio.url=${DIRECTORIO_URL:http://10.43.100.251:8200}
       directorio.api-key=${DIRECTORIO_API_KEY:clave-ad-2026}

3. En tu login actual, reemplaza la validación de usuario y clave por esto (conserva tu emisión de token):

       try {
           DatosUsuario u = directorio.autenticar(p.usuario(), p.contrasena());
           // Crea o actualiza el cliente en la BD del SSO con u.cedula(), u.nombre(), u.correo(), u.celular(), u.direccion().
           // Los necesitas para OFAC (cédula), correo al ganador, SMS (celular) y envío SEN (dirección).
           return Map.of("token", tokenService.crear(u.usuario()), "nombre", u.nombre());
       } catch (DirectorioRechazoException e) {   // 401: usuario o clave incorrectos
           return ResponseEntity.status(e.getEstado()).body(Map.of("mensaje", e.getMessage()));
       } catch (DirectorioNoDisponibleException e) {   // 503: nunca devolver 401 si el directorio está caído
           return ResponseEntity.status(503).body(Map.of("mensaje", e.getMessage()));
       }

4. `/api/auth/registro` ya queda atendido por `RegistroController`. Debe ser público, como el login.
5. El token debe ser válido en las dos instancias (8000 y 8080): misma clave secreta en ambas.
