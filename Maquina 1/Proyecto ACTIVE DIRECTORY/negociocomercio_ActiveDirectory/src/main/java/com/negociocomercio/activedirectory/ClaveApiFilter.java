package com.negociocomercio.activedirectory;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

/** Solo el monolito (que conoce la clave) puede usar el directorio. /ad/estado queda abierto. */
@Component
public class ClaveApiFilter extends OncePerRequestFilter {

    private final byte[] clave;

    public ClaveApiFilter(@Value("${ad.api-key}") String clave) {
        this.clave = clave.getBytes(StandardCharsets.UTF_8);
    }

    @Override
    protected boolean shouldNotFilter(HttpServletRequest request) {
        String ruta = request.getRequestURI();
        return !ruta.startsWith("/ad/") || ruta.equals("/ad/estado");
    }

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain chain)
            throws ServletException, IOException {
        String enviada = request.getHeader("X-Api-Key");
        boolean valida = enviada != null
                && MessageDigest.isEqual(enviada.getBytes(StandardCharsets.UTF_8), clave);
        if (!valida) {
            response.setStatus(HttpServletResponse.SC_FORBIDDEN);
            response.setContentType("application/json;charset=UTF-8");
            response.getWriter().write("{\"mensaje\":\"Clave de acceso al directorio no válida.\"}");
            return;
        }
        chain.doFilter(request, response);
    }
}
