package com.sen.service;

import com.sen.dto.CallbackDTO;
import com.sen.dto.CompraDTO;
import com.sen.entity.EntitySEN;
import com.sen.proxy.ProxyCallbackSEN;
import com.sen.repository.RepositorySEN;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture;

@RestController
@RequestMapping("/api/sen")
public class ServiceSEN {

    private final RepositorySEN repositorySEN;
    private final ProxyCallbackSEN proxyCallbackSEN;

    public ServiceSEN(RepositorySEN repositorySEN, ProxyCallbackSEN proxyCallbackSEN) {
        this.repositorySEN = repositorySEN;
        this.proxyCallbackSEN = proxyCallbackSEN;
    }

    @PostMapping("/recibir")
    public ResponseEntity<Map<String, Object>> recibir(@RequestBody CompraDTO compra) {
        if (compra == null || compra.getIdCompra() == null
                || compra.getBienes() == null || compra.getDireccion() == null) {
            return ResponseEntity.badRequest()
                    .body(Map.of("ok", false, "mensaje", "Datos incompletos"));
        }

        int guardados = 0;
        for (CompraDTO.BienDTO b : compra.getBienes()) {
            if (repositorySEN.existsByIdCompraAndIdBien(compra.getIdCompra(), b.getIdBien())) {
                continue;
            }
            EntitySEN e = new EntitySEN();
            e.setIdCompra(compra.getIdCompra());
            e.setCedula(compra.getCedula());
            e.setDireccion(compra.getDireccion());
            e.setIdBien(b.getIdBien());
            e.setDescripcion(b.getDescripcion());
            e.setEstadoEnvio("RECIBIDO");
            repositorySEN.save(e);
            guardados++;
        }

        final int guardadosFinal = guardados;
        CompletableFuture.runAsync(() -> enviarCallback(compra.getIdCompra(), true,
                "SEN programó " + guardadosFinal + " envío(s)"));

        return ResponseEntity.accepted().body(Map.of(
                "ok", true,
                "sistema", "SEN",
                "idCompra", compra.getIdCompra(),
                "programados", guardados,
                "mensaje", "Recibido. Callback será enviado al SSO."
        ));
    }

    private void enviarCallback(Long idCompra, boolean ok, String mensaje) {
        CallbackDTO dto = new CallbackDTO(idCompra, "SEN", ok, mensaje);
        proxyCallbackSEN.notificar(dto)
                .doOnSuccess(r -> System.out.println("[SEN] Callback enviado OK para compra " + idCompra))
                .doOnError(e -> System.out.println("[SEN] Fallo callback compra " + idCompra + ": " + e.getMessage()))
                .onErrorResume(e -> reactor.core.publisher.Mono.empty())
                .subscribe();
    }

    @GetMapping("/envios")
    public List<EntitySEN> listar() { return repositorySEN.findAll(); }

    @GetMapping("/envios/{idCompra}")
    public List<EntitySEN> porCompra(@PathVariable Long idCompra) {
        return repositorySEN.findByIdCompra(idCompra);
    }

    @GetMapping("/health")
    public String health() { return "SEN-UP"; }
}