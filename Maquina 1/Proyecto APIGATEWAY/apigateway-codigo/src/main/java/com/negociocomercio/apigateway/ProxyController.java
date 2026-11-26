package com.negociocomercio.apigateway;

import jakarta.servlet.http.HttpServletRequest;
import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.Collections;
import java.util.Set;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.http.client.JdkClientHttpRequestFactory;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.client.RestClient;
import org.springframework.web.client.RestClientException;

/**
 * Reenvía todo lo que llega a /api/** hacia el upstream (NGINX interno que balancea hacia la M2).
 * No interpreta ni modifica el contenido: solo pasa método, ruta, cabeceras y cuerpo.
 * Esto incluye /api/yopago/confirmacion, que es la llamada que YoPago hace al SSO.
 */
@RestController
public class ProxyController {

    private static final Logger log = LoggerFactory.getLogger(ProxyController.class);

    private static final Set<String> NO_REENVIAR = Set.of(
            "host", "connection", "keep-alive", "proxy-authenticate", "proxy-authorization",
            "te", "trailer", "transfer-encoding", "upgrade", "content-length", "expect",
            "accept-encoding");

    private static final String[] CABECERAS_DE_RESPUESTA = {
            "Content-Type", "Cache-Control", "Location", "WWW-Authenticate", "Retry-After" };

    private final RestClient cliente;
    private final String upstream;

    public ProxyController(@Value("${gateway.upstream}") String upstream) {
        this.upstream = upstream.endsWith("/") ? upstream.substring(0, upstream.length() - 1) : upstream;
        HttpClient http = HttpClient.newBuilder()
                .version(HttpClient.Version.HTTP_1_1)
                .connectTimeout(Duration.ofSeconds(3))
                .build();
        JdkClientHttpRequestFactory fabrica = new JdkClientHttpRequestFactory(http);
        fabrica.setReadTimeout(Duration.ofSeconds(15));
        this.cliente = RestClient.builder().requestFactory(fabrica).build();
    }

    @RequestMapping("/api/**")
    public ResponseEntity<byte[]> reenviar(HttpServletRequest request) throws IOException {
        long inicio = System.currentTimeMillis();
        String consulta = request.getQueryString();
        URI destino = URI.create(upstream + request.getRequestURI() + (consulta != null ? "?" + consulta : ""));
        byte[] cuerpo = request.getInputStream().readAllBytes();

        try {
            RestClient.RequestBodySpec peticion = cliente
                    .method(HttpMethod.valueOf(request.getMethod()))
                    .uri(destino)
                    .headers(h -> copiarCabeceras(request, h));
            if (cuerpo.length > 0) {
                peticion = peticion.body(cuerpo);
            }

            ResponseEntity<byte[]> respuesta = peticion.exchange((req, res) -> {
                byte[] datos = res.getBody().readAllBytes();
                return ResponseEntity.status(res.getStatusCode())
                        .headers(h -> {
                            for (String nombre : CABECERAS_DE_RESPUESTA) {
                                String valor = res.getHeaders().getFirst(nombre);
                                if (valor != null) {
                                    h.add(nombre, valor);
                                }
                            }
                            h.add("X-Gateway", "ApiGateway-M1");
                        })
                        .body(datos);
            });

            log.info("{} {} -> {} ({} ms)", request.getMethod(), request.getRequestURI(),
                    respuesta.getStatusCode().value(), System.currentTimeMillis() - inicio);
            return respuesta;

        } catch (RestClientException e) {
            log.warn("{} {} -> upstream no disponible: {}", request.getMethod(), request.getRequestURI(), e.getMessage());
            byte[] json = "{\"mensaje\":\"El servicio no está disponible en este momento. Intenta de nuevo en unos minutos.\"}"
                    .getBytes(StandardCharsets.UTF_8);
            return ResponseEntity.status(502).contentType(MediaType.APPLICATION_JSON).body(json);
        }
    }

    private static void copiarCabeceras(HttpServletRequest request, HttpHeaders destino) {
        for (String nombre : Collections.list(request.getHeaderNames())) {
            if (NO_REENVIAR.contains(nombre.toLowerCase())) {
                continue;
            }
            for (String valor : Collections.list(request.getHeaders(nombre))) {
                destino.add(nombre, valor);
            }
        }
    }
}
