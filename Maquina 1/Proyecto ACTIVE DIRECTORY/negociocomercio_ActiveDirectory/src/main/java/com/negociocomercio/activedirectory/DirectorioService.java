package com.negociocomercio.activedirectory;

import java.util.Optional;
import org.springframework.http.HttpStatus;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

@Service
public class DirectorioService {

    // Hash de una clave cualquiera: se compara cuando el usuario no existe para que
    // "usuario inexistente" y "clave incorrecta" tarden lo mismo y no se pueda distinguirlos.
    private static final String HASH_RELLENO = new BCryptPasswordEncoder().encode("relleno-no-usar");

    private final UsuarioRepository repo;
    private final BCryptPasswordEncoder codificador = new BCryptPasswordEncoder();

    public DirectorioService(UsuarioRepository repo) {
        this.repo = repo;
    }

    @Transactional
    public void crear(RegistroPeticion p) {
        String usuario = p.usuario().trim().toLowerCase();
        String cedula = p.cedula().trim();
        if (repo.existsByUsuario(usuario)) {
            throw new ErrorApi(HttpStatus.CONFLICT, "Ese usuario ya existe. Elige otro.");
        }
        if (repo.existsByCedula(cedula)) {
            throw new ErrorApi(HttpStatus.CONFLICT, "Ya existe una cuenta con esa cédula.");
        }
        UsuarioDirectorio u = new UsuarioDirectorio();
        u.setUsuario(usuario);
        u.setCedula(cedula);
        u.setNombre(p.nombre().trim());
        u.setCorreo(p.correo().trim());
        u.setCelular(p.celular().trim());
        u.setDireccion(p.direccion().trim());
        u.setClaveHash(codificador.encode(p.contrasena()));
        repo.save(u);
    }

    @Transactional(readOnly = true)
    public DatosUsuario autenticar(String usuario, String contrasena) {
        Optional<UsuarioDirectorio> encontrado = repo.findByUsuario(usuario.trim().toLowerCase());
        String hash = encontrado.map(UsuarioDirectorio::getClaveHash).orElse(HASH_RELLENO);
        boolean correcta = codificador.matches(contrasena, hash);
        if (encontrado.isEmpty() || !correcta || !encontrado.get().isActivo()) {
            throw new ErrorApi(HttpStatus.UNAUTHORIZED, "Usuario o contraseña incorrectos.");
        }
        return DatosUsuario.de(encontrado.get());
    }

    /** Usado solo por el sembrado de pruebas. */
    @Transactional
    public void sembrar(String usuario, String cedula, String nombre, String correo, String celular,
                        String direccion, String clave) {
        Optional<UsuarioDirectorio> existente = repo.findByUsuario(usuario);
        if (existente.isPresent()) {
            // Si el usuario de prueba se creó antes de existir la dirección, se completa.
            if (existente.get().getDireccion() == null) {
                existente.get().setDireccion(direccion);
                repo.save(existente.get());
            }
            return;
        }
        UsuarioDirectorio u = new UsuarioDirectorio();
        u.setUsuario(usuario);
        u.setCedula(cedula);
        u.setNombre(nombre);
        u.setCorreo(correo);
        u.setCelular(celular);
        u.setDireccion(direccion);
        u.setClaveHash(codificador.encode(clave));
        repo.save(u);
    }
}
