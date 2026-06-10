#!/usr/bin/env python3
import http.server, json, os, subprocess, threading

TOKEN  = "c84a12e9f3b507d6e1a28c4f90d37b58e2461785a3c9d012b6f48e7059a1c3d4"
DEPLOY = "/opt/gestorai-deploy/deploy.sh"
PORT   = 9989
lock   = threading.Lock()

class Handler(http.server.BaseHTTPRequestHandler):
    def log_message(self, *_): pass

    def do_POST(self):
        if self.path != "/deploy":
            self.send_response(404); self.end_headers(); return

        if self.headers.get("X-Deploy-Token") != TOKEN:
            self.send_response(403); self.end_headers(); return

        length = int(self.headers.get("Content-Length", 0))
        body   = json.loads(self.rfile.read(length) or b"{}")
        sha    = body.get("sha", "").strip()

        if not sha:
            self.send_response(400)
            self.send_header("Content-Type", "application/json"); self.end_headers()
            self.wfile.write(b'{"status":"error","output":"sha ausente"}')
            return

        if not lock.acquire(blocking=False):
            self.send_response(409)
            self.send_header("Content-Type", "application/json"); self.end_headers()
            self.wfile.write(b'{"status":"error","output":"deploy ja em andamento"}')
            return

        try:
            result = subprocess.run(
                ["bash", DEPLOY, sha],
                capture_output=True, text=True, timeout=300
            )
            output = result.stdout + result.stderr
            ok     = result.returncode == 0
            self.send_response(200)
            self.send_header("Content-Type", "application/json"); self.end_headers()
            self.wfile.write(json.dumps({
                "status": "ok" if ok else "error",
                "output": output[-2000:]
            }).encode())
        except subprocess.TimeoutExpired:
            self.send_response(200)
            self.send_header("Content-Type", "application/json"); self.end_headers()
            self.wfile.write(b'{"status":"error","output":"timeout"}')
        finally:
            lock.release()

if __name__ == "__main__":
    srv = http.server.HTTPServer(("127.0.0.1", PORT), Handler)
    print(f"gestorai-deploy webhook listening on 127.0.0.1:{PORT}")
    srv.serve_forever()
