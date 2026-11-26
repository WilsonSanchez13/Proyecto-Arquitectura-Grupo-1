package com.negociocomercio.mssso.directorio;

import java.net.http.HttpClient;
import java.time.Duration;
import java.util.Map;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.MediaType;
import org.springframework.http.client.JdkClientHttpRequestFactory;
import org.springframework.stereotype.Component;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.HttpServerErrorException;
import org.springframework.web.client.ResourceAccessException;
import org.springframework.web.client.RestClient;

/**
 * Único punto por donde el monolito habla con el Active Directory.
 * No guarda contraseñas: las envía al directorio y usa su respuesta.
 */
@Component
public class DirectorioActivoClient {

    private static final Logger log = LoggerFactory.getLogger(DirectorioActivoClient.class);

    private final RestClient http;

    public DirectorioActivoClient(@Value("${directorio.url}") String url,
                                  @Value("${directorio.api-key}") String clave) {
        HttpClient cliente = HttpClient.newBuilder()
                .version(HttpClient.Version.HTTP_1_1)
                .connectTimeout(Duration.ofSeconds(3))
                .build();
        JdkClientHttpRequestFactory fabrica = new JdkClientHttpRequestFactory(cliente);
        fabrica.setReadTimeout(Duration.ofSeconds(5));
        this.http = RestClient.builder()
                .baseUrl(url)
                .requestFactory(fabrica)
                .defaultHeader("X-Api-Key", clave)
                .build();
    }

    /** Devuelve los datos del cliente si el usuario y la clave son correctos; si no, lanza DirectorioRechazoException (401). */
    public DatosUsuario autenticar(String usuario, String contrasena) {
        return enviar("/ad/autenticar", Map.of("usuario", usuario, "contrasena", contrasena), DatosUsuario.class);
    }

    /** Crea la cuenta con todos los datos. Lanza DirectorioRechazoException (400 o 409) si el directorio la rechaza. */
    public void registrar(RegistroPeticion p) {
        enviar("/ad/usuarios", p, Map.class);
    }

    private <T> T enviar(String ruta, Object cuerpo, Class<T> tipo) {
        try {
            return http.post().uri(ruta).contentType(MediaType.APPLICATION_JSON).body(cuerpo).retrieve().body(tipo);
        } catch (HttpClientErrorException.Forbidden e) {
            log.error("El directorio rechazó la clave de API: revisa directorio.api-key");
            throw new DirectorioNoDisponibleException();
        } catch (HttpClientErrorException e) {
            throw new DirectorioRechazoException(e.getStatusCode().value(), mensajeDe(e));
        } catch (HttpServerErrorException | ResourceAccessException e) {
            log.warn("Directorio no disponible: {}", e.getMessage());
            throw new DirectorioNoDisponibleException();
        }
    }

    private String mensajeDe(HttpClientErrorException e) {
        try {
            Map<?, ?> cuerpo = e.getResponseBodyAs(Map.class);
            Object mensaje = cuerpo == null ? null : cuerpo.get("mensaje");
            if (mensaje != null) { return mensaje.toString(); }
        } catch (RuntimeException ignorada) {
            // si el cuerpo no es JSON se usa el texto por defecto
        }
        return "El directorio rechazó la solicitud.";
    }
}
