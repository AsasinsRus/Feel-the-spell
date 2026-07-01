import socket
import threading

class TCPServer:
    
    def __init__(self, host="0.0.0.0", port=65432):
        self.conn = None
        self._lock = threading.Lock()
        
        self._server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self._server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self._server.bind((host, port))
        self._server.listen()

        print(f"Listening on {host}:{port}...")

        threading.Thread(target=self._accept, args=(self._server,), daemon=True).start()

    def _accept(self, server):
        while True:
            conn, addr = server.accept()
            with self._lock:
                self.conn = conn
            print(f"Client connected: {addr}")

    def send(self, message: str):
        with self._lock:
            if self.conn is None:
                print("No client connected")
                return
            
            try:
                self.conn.sendall(message.encode())
            except OSError as e:
                print(f"Send failed: {e}")
                self.conn = None

    def close(self):
        with self._lock:
            if self.conn:
                self.conn.close()
        self._server.close()

    def __enter__(self):
        return self

    def __exit__(self, *args):
        self.close()
