package com.negociocomercio.activedirectory;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.CommandLineRunner;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class SembradoPrueba {

    @Bean
    CommandLineRunner sembrar(DirectorioService servicio, @Value("${ad.sembrar-prueba}") boolean activo) {
        return args -> {
            if (activo) {
                servicio.sembrar("cliente", "1000000001", "Cliente de prueba",
                        "cliente@ejemplo.com", "3000000000", "Calle 1 # 2-3, Bogotá", "1234");
            }
        };
    }
}
