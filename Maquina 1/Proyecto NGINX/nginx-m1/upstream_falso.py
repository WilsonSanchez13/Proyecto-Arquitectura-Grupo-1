import json
import sys
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

# Uso: python3 upstream_falso.py 9001
PUERTO = int(sys.argv[1]) if len(sys.argv) > 1 else 9001


class Manejador(BaseHTTPRequestHandler):
    protocol_version = 'HTTP/1.1'

    def _responder(self):
        largo = int(self.headers.get('Content-Length') or 0)
        cuerpo = self.rfile.read(largo).decode('utf-8') if largo else ''
        if self.path.startswith('/api/error'):
            codigo = 400
            datos = {'mensaje': 'Error de prueba desde el upstream'}
        else:
            codigo = 200
            datos = {
                'upstream': 'falso',
                'puerto': PUERTO,
                'metodo': self.command,
                'ruta': self.path,
                'authorization': self.headers.get('Authorization'),
                'contentType': self.headers.get('Content-Type'),
                'cuerpo': cuerpo,
            }
        crudo = json.dumps(datos, ensure_ascii=False).encode('utf-8')
        self.send_response(codigo)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(crudo)))
        self.end_headers()
        self.wfile.write(crudo)
        print('[falso ' + str(PUERTO) + ']', self.command, self.path, '->', codigo, flush=True)

    do_GET = do_POST = do_PUT = do_DELETE = _responder

    def log_message(self, *args):
        pass


if __name__ == '__main__':
    print('Upstream falso escuchando en el puerto', PUERTO, flush=True)
    ThreadingHTTPServer(('0.0.0.0', PUERTO), Manejador).serve_forever()
