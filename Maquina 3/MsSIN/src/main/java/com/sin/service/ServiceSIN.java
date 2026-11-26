package com.sin.service;

import com.sin.dto.CallbackDTO;
import com.sin.dto.CompraDTO;
import com.sin.entity.EntitySIN;
import com.sin.proxy.ProxyCallbackSIN;
import com.sin.repository.RepositorySIN;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

@RestController
@RequestMapping("/api/sin")
public class ServiceSIN {

    private final RepositorySIN repositorySIN;
    private final ProxyCallbackSIN proxyCallbackSIN;

    public ServiceSIN(RepositorySIN repositorySIN, ProxyCallbackSIN proxyCallbackSIN) {
        this.repositorySIN = repositorySIN;
        this.proxyCallbackSIN = proxyCallbackSIN;
    }

    /**
     * SSO envía la compra. Respondemos 202 de inmediato y el callback
     * se dispara en background cuando ya se guardó todo.
     */
    @PostMapping("/recibir")
    public ResponseEntity<Map<String, Object>> recibir(@RequestBody CompraDTO compra) {
        if (compra == null || compra.getIdCompra() == null || compra.getBienes() == null) {
            return ResponseEntity.badRequest()
                    .body(Map.of("ok", false, "mensaje", "Datos incompletos"));
        }

        // 1. Guardar (idempotente)
        int guardados = 0;
        for (CompraDTO.BienDTO b : compra.getBienes()) {
            if (repositorySIN.existsByIdCompraAndIdBien(compra.getIdCompra(), b.getIdBien())) {
                continue;
            }
            EntitySIN e = new EntitySIN();
            e.setIdCompra(compra.getIdCompra());
            e.setCedula(compra.getCedula());
            e.setIdBien(b.getIdBien());
            e.setDescripcion(b.getDescripcion());
            e.setCantidad(b.getCantidad() != null ? b.getCantidad() : 1);
            e.setEstado("RECIBIDO");
            repositorySIN.save(e);
            guardados++;
        }

        // 2. Disparar callback al SSO en background (no bloquea la respuesta)
        final int guardadosFinal = guardados;
        CompletableFuture.runAsync(() -> enviarCallback(compra.getIdCompra(), true,
                "SIN guardó " + guardadosFinal + " bien(es)"));

        // 3. Responder 202 Accepted
        return ResponseEntity.accepted().body(Map.of(
                "ok", true,
                "sistema", "SIN",
                "idCompra", compra.getIdCompra(),
                "guardados", guardados,
                "mensaje", "Recibido. Callback será enviado al SSO."
        ));
    }

    private void enviarCallback(Long idCompra, boolean ok, String mensaje) {
        CallbackDTO dto = new CallbackDTO(idCompra, "SIN", ok, mensaje);
        proxyCallbackSIN.notificar(dto)
                .doOnSuccess(r -> System.out.println("[SIN] Callback enviado OK para compra " + idCompra))
                .doOnError(e -> System.out.println("[SIN] Fallo callback compra " + idCompra + ": " + e.getMessage()))
                .onErrorResume(e -> reactor.core.publisher.Mono.empty())
                .subscribe();
    }

    @GetMapping("/compras")
    public List<EntitySIN> listar() { return repositorySIN.findAll(); }

    @GetMapping("/compras/{idCompra}")
    public List<EntitySIN> porCompra(@PathVariable Long idCompra) {
        return repositorySIN.findByIdCompra(idCompra);
    }

    @GetMapping("/health")
    public String health() { return "SIN-UP"; }
}