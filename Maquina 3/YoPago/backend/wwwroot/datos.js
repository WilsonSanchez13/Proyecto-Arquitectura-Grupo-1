// Pinta en tablas lo que devuelve GET /api/datos-desarrollo. Usa textContent (nunca innerHTML con datos).
const moneda = new Intl.NumberFormat('es-CO', { maximumFractionDigits: 0 });
const fecha = (v) => (v ? new Date(v).toLocaleString('es-CO', { dateStyle: 'medium', timeStyle: 'short' }) : '—');
const pesos = (v) => `$ ${moneda.format(v)}`;
const texto = (v) => (v === null || v === undefined || v === '' ? '—' : String(v));

const TABLAS = {
  directorio: { datos: 'directorio', cuenta: 'n-directorio', columnas: [['Cédula', 'cedula'], ['Clave (texto plano)', 'clave']] },
  clientes: { datos: 'clientes', cuenta: 'n-clientes', columnas: [
    ['Cédula', 'cedula'], ['Nombre', 'nombre'], ['Correo', 'correo'],
    ['Creado', 'creadoEn', fecha], ['Bloqueado hasta', 'bloqueadoHasta', fecha]] },
  compras: { datos: 'compras', cuenta: 'n-compras', columnas: [
    ['N.º compra', 'numeroCompra'], ['Cédula', 'cedula'], ['Bien', 'descripcion'], ['Subasta', 'codigoSubasta'],
    ['Oferta', 'valorOferta', pesos, 'num'], ['Impuestos', 'impuestos', pesos, 'num'], ['Total', 'valorTotal', pesos, 'num'],
    ['Estado', 'estado', null, 'estado'], ['Recibida', 'recibidaEn', fecha], ['Vence', 'venceEn', fecha], ['Pagada', 'pagadaEn', fecha]] },
  pagos: { datos: 'pagos', cuenta: 'n-pagos', columnas: [
    ['Transacción', 'codigoTransaccion'], ['Compra', 'numeroCompra'], ['Medio', 'medioPago'], ['Banco', 'banco'],
    ['Valor', 'valor', pesos, 'num'], ['Estado', 'estado', null, 'estado'], ['Código SSO', 'codigoAcuseSso'], ['Fecha', 'creadoEn', fecha]] },
  cola: { datos: 'mensajes', cuenta: 'n-cola', columnas: [
    ['Tipo', 'tipo'], ['Creado', 'creadoEn', fecha], ['Enviado a SSO', 'procesadoEn', fecha],
    ['Intentos', 'intentos'], ['Último error', 'ultimoError']] },
};

function pintar(id, def, filas) {
  const tabla = document.getElementById(`tabla-${id}`);
  document.getElementById(def.cuenta).textContent = filas.length;
  tabla.replaceChildren();

  const thead = tabla.createTHead().insertRow();
  def.columnas.forEach(([titulo]) => {
    const th = document.createElement('th');
    th.scope = 'col';
    th.textContent = titulo;
    thead.append(th);
  });

  const cuerpo = tabla.createTBody();
  if (filas.length === 0) {
    const td = cuerpo.insertRow().insertCell();
    td.colSpan = def.columnas.length;
    td.className = 'vacio';
    td.textContent = 'Sin datos.';
    return;
  }
  filas.forEach((fila) => {
    const tr = cuerpo.insertRow();
    def.columnas.forEach(([, campo, formato, clase]) => {
      const td = tr.insertCell();
      const valor = formato ? formato(fila[campo]) : texto(fila[campo]);
      if (clase === 'estado') {
        const span = document.createElement('span');
        span.className = `estado estado--${fila[campo]}`;
        span.textContent = valor;
        td.append(span);
      } else {
        td.textContent = fila[campo] === null || fila[campo] === undefined ? '—' : valor;
        if (clase) td.className = clase;
      }
    });
  });
}

async function cargar() {
  const estado = document.getElementById('estado');
  estado.className = '';
  estado.textContent = 'Cargando…';
  try {
    const r = await fetch('/api/datos-desarrollo');
    if (r.status === 404) throw new Error('La vista de datos está desactivada (DatosDesarrollo:Habilitado = false).');
    if (!r.ok) throw new Error(`La API respondió ${r.status}.`);
    const d = await r.json();

    document.getElementById('prueba-cedula').textContent = d.usuarioPrueba.cedula;
    document.getElementById('prueba-clave').textContent = d.usuarioPrueba.clave;
    document.getElementById('usuario-prueba').hidden = false;

    Object.entries(TABLAS).forEach(([id, def]) => pintar(id, def, d[def.datos]));
    estado.textContent = `Actualizado a las ${new Date().toLocaleTimeString('es-CO')}`;
  } catch (e) {
    estado.className = 'error';
    estado.textContent = e.message;
  }
}

document.getElementById('actualizar').addEventListener('click', cargar);
cargar();
