import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService, mensajeDeError } from '../api.service';
import { AccesoMarco } from '../compartidos/acceso-marco';

// Solo recoge los datos y los envía. Las reglas (cédula válida, usuario libre,
// seguridad de la contraseña) las decide el backend y aquí se muestra su mensaje.
@Component({
  selector: 'app-registro',
  imports: [FormsModule, RouterLink, AccesoMarco],
  template: `
    <app-acceso-marco>
      @if (creada()) {
        <h2>Cuenta creada</h2>
        <p class="aviso" role="status">{{ creada() }}</p>
        <a class="boton ancho" routerLink="/login">Ir a ingresar</a>
      } @else {
        <h2>Crea tu cuenta</h2>
        <label>Nombre completo
          <input [(ngModel)]="nombre" autocomplete="name" />
        </label>
        <div class="par">
          <label>Cédula
            <input [(ngModel)]="cedula" inputmode="numeric" autocomplete="off" />
          </label>
          <label>Celular
            <input [(ngModel)]="celular" inputmode="tel" autocomplete="tel" />
          </label>
        </div>
        <label>Correo electrónico
          <input type="email" [(ngModel)]="correo" autocomplete="email" />
        </label>
        <label>Dirección de envío
          <input [(ngModel)]="direccion" autocomplete="street-address" placeholder="Calle, número, ciudad" />
        </label>
        <label>Usuario
          <input [(ngModel)]="usuario" autocomplete="username" />
        </label>
        <div class="par">
          <label>Contraseña
            <input type="password" [(ngModel)]="contrasena" autocomplete="new-password" />
          </label>
          <label>Repite la contraseña
            <input type="password" [(ngModel)]="repetida" autocomplete="new-password" (keyup.enter)="crear()" />
          </label>
        </div>
        @if (repetida() !== '' && contrasena() !== repetida()) {
          <p class="error" role="alert">Las contraseñas no coinciden.</p>
        }
        <p class="suave">Usaremos tu correo y tu celular para avisarte cuando ganes una subasta, y tu dirección para programar el envío.</p>
        <button class="ancho" (click)="crear()" [disabled]="!completo() || cargando()">Crear cuenta</button>
        @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
        <p class="alterno">¿Ya tienes cuenta? <a routerLink="/login">Ingresar</a></p>
      }
    </app-acceso-marco>
  `,
})
export class Registro {
  private api = inject(ApiService);

  nombre = signal('');
  cedula = signal('');
  celular = signal('');
  correo = signal('');
  direccion = signal('');
  usuario = signal('');
  contrasena = signal('');
  repetida = signal('');
  error = signal('');
  creada = signal('');
  cargando = signal(false);

  completo = computed(
    () =>
      [this.nombre(), this.cedula(), this.celular(), this.correo(), this.direccion(), this.usuario(), this.contrasena()]
        .every((v) => v.trim() !== '') && this.contrasena() === this.repetida(),
  );

  crear(): void {
    if (!this.completo()) { return; }
    this.cargando.set(true);
    this.error.set('');
    this.api
      .registrar({
        nombre: this.nombre().trim(),
        cedula: this.cedula().trim(),
        correo: this.correo().trim(),
        celular: this.celular().trim(),
        direccion: this.direccion().trim(),
        usuario: this.usuario().trim(),
        contrasena: this.contrasena(),
      })
      .subscribe({
        next: (r) => this.creada.set(r.mensaje),
        error: (e: HttpErrorResponse) => {
          this.error.set(mensajeDeError(e));
          this.cargando.set(false);
        },
      });
  }
}
