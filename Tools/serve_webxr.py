#!/usr/bin/env python3
"""Serve the Quest-browser (WebXR) build of Gorilla Company to your home Wi-Fi.

Usage (from the project folder):   python3 Tools/serve_webxr.py
Then on the Quest: open Browser, go to the https://... address it prints, tap Advanced > Proceed, press the VR button.
Stop it with Ctrl+C. Only devices on your own network can reach it, and only while this is running.

WebXR only works on "secure" (https) pages, so this makes a home-made certificate the first time
(saved in Builds/cert, which git ignores). The Quest browser warns about it once - that's expected.
"""
import http.server
import os
import socket
import ssl
import subprocess
import sys

PORT = 8443
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
SITE = os.path.join(ROOT, "Builds", "WebXR")
CERT_DIR = os.path.join(ROOT, "Builds", "cert")


def lan_ip():
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect(("10.255.255.255", 1))   # no data is sent; this just picks the Wi-Fi address
        return s.getsockname()[0]
    except OSError:
        return "127.0.0.1"
    finally:
        s.close()


def make_cert(ip):
    os.makedirs(CERT_DIR, exist_ok=True)
    cert, key = os.path.join(CERT_DIR, "cert.pem"), os.path.join(CERT_DIR, "key.pem")
    if not (os.path.exists(cert) and os.path.exists(key)):
        subprocess.run(["openssl", "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "825",
                        "-keyout", key, "-out", cert, "-subj", "/CN=gorilla-company-local",
                        "-addext", f"subjectAltName=IP:{ip},IP:127.0.0.1,DNS:localhost"],
                       check=True, capture_output=True)
    return cert, key


class Handler(http.server.SimpleHTTPRequestHandler):
    extensions_map = {**http.server.SimpleHTTPRequestHandler.extensions_map,
                      ".wasm": "application/wasm", ".js": "application/javascript",
                      ".unityweb": "application/octet-stream", ".data": "application/octet-stream"}

    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=SITE, **kwargs)


def main():
    if not os.path.exists(os.path.join(SITE, "index.html")):
        sys.exit("No web build found. In Unity: Gorilla Company > Build > Quest Browser (WebXR).")
    ip = lan_ip()
    cert, key = make_cert(ip)
    server = http.server.ThreadingHTTPServer(("0.0.0.0", PORT), Handler)
    ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER)
    ctx.load_cert_chain(cert, key)
    server.socket = ctx.wrap_socket(server.socket, server_side=True)
    print(f"\n  Gorilla Company is being served.\n  On the Quest Browser, go to:   https://{ip}:{PORT}\n"
          f"  (tap Advanced > Proceed on the warning, then press the VR button)\n  Press Ctrl+C to stop.\n", flush=True)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass


if __name__ == "__main__":
    main()
