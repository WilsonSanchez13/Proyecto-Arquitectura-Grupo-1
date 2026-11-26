package com.negociocomercio.apigateway;

import java.util.Map;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class EstadoController {

    private final String upstream;

    public EstadoController(@Value("${gateway.upstream}") String upstream) {
        this.upstream = upstream;
    }

    @GetMapping("/gateway/estado")
    public Map<String, String> estado() {
        return Map.of("servicio", "ApiGateway", "estado", "activo", "upstream", upstream);
    }
}
