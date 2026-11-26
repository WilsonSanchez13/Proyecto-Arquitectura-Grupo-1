export interface LoginPeticion { usuario: string; contrasena: string; }
export interface LoginRespuesta { token: string; nombre: string; }

export interface RegistroPeticion {
  nombre: string;
  cedula: string;
  correo: string;
  celular: string;
  direccion: string;
  usuario: string;
  contrasena: string;
}

export interface Bien {
  id: number;
  nombre: string;
  descripcion: string;
  valorMinimo: number;
  fechaInicio: string;
  fechaCierre: string;
  ofertaMasAlta: number | null;
}

export interface ItemCarrito { bienId: number; nombre: string; valorMinimo: number; }
export interface ItemOferta { bienId: number; valor: number; }

export interface Oferta {
  id: number;
  bienNombre: string;
  valorOfertado: number;
  estado: string;
  fecha: string;
}

export interface Suscripciones { nuevasSubastas: boolean; cambiosValores: boolean; }

export interface Compra {
  id: number;
  bienNombre: string;
  valorOferta: number;
  impuestos: number;
  total: number;
  estado: string;
  puedeIniciar: boolean;
  urlPago: string | null;
}

export interface Mensaje { mensaje: string; }
