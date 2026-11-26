package com.ofac.timmer;

import com.ofac.entity.EntityOFAC;
import com.ofac.repository.RepositoryOFAC;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Random;

/**
 * TimmerReintento: cada 15 segundos revisa los registros en ENPROCESO
 * y los resuelve (simula que la OFAC externa ya terminó de verificar).
 * Regla: 80% APROBADO, 20% RECHAZADO.
 */
@Component
public class TimmerReintento {

    private final RepositoryOFAC repositoryOFAC;
    private final Random random = new Random();

    public TimmerReintento(RepositoryOFAC repositoryOFAC) {
        this.repositoryOFAC = repositoryOFAC;
    }

    @Scheduled(fixedDelay = 15000) // cada 15 segundos
    public void reintentarEnProceso() {
        List<EntityOFAC> pendientes = repositoryOFAC.findByEstado("ENPROCESO");

        if (pendientes.isEmpty()) {
            return; // nada por hacer
        }

        System.out.println("[TimmerReintento] Resolviendo "
                + pendientes.size() + " registro(s) ENPROCESO...");

        for (EntityOFAC e : pendientes) {
            String nuevoEstado = random.nextDouble() < 0.80 ? "APROBADO" : "RECHAZADO";
            e.setEstado(nuevoEstado);
            repositoryOFAC.save(e);
            System.out.println("[TimmerReintento] cédula " + e.getCedula()
                    + " -> " + nuevoEstado);
        }
    }
}
