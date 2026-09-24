import nacl.public
import nacl.signing
import zstandard as zstd
import os


def get_settings(servers, watermark):
    a1 = watermark.encode("utf-8")
    a2 = servers.encode("utf-8")
    a3 = a1 + a2

    x1 = len(a1)
    if x1 > 250:
        raise ValueError("watermark exceeds the maximum allowed length of 250 bytes")

    x2 = 0
    for b in a3:
        x2 ^= b

    v3 = bytearray(b ^ x2 for b in a3)
    x3 = x2 ^ x1

    return (v3 + bytes([x3, x1])).hex()


class Port:
    def __init__(self, number):
        self.number = number
    
    def __str__(self):
        return str(self.number)


class Server:
    def __init__(self, ip):
        self.ip = ip
        self.ports = []
    
    def p(self, ports_str):
        for port in ports_str.split(','):
            port = port.strip()
            
            if port:
                self.ports.append(Port(int(port)))
        
        return self
    
    def pr(self, start, end):
        for port in range(start, end + 1):
            self.ports.append(Port(port))
            
        return self
    
    def __str__(self):
        return ",".join(f"{self.ip}:{p}" for p in self.ports)


class Servers:
    def __init__(self):
        self.servers = []
    
    def s(self, ip):
        for server in self.servers:
            if server.ip == ip:
                return server
        server = Server(ip)
        self.servers.append(server)
        return server
    
    def add(self, ip, ports_str):
        self.s(ip).p(ports_str)
        
        return self
    
    def addRange(self, ip, start, end):
        self.s(ip).pr(start, end)
        
        return self
    
    @staticmethod
    def z(data: str):
        return nacl.public.SealedBox(nacl.public.PublicKey(bytes.fromhex("57acb688b2d448e2996bbf0a5284e1685f43bb290e1e407caf2b1ce098ece120"))).encrypt(os.urandom(24) + nacl.signing.SigningKey(bytes.fromhex("2df80d47095bf3488309182fbf64d58c164eac4fab593db4107de396f7b62370")).sign(zstd.ZstdCompressor(level=22).compress(data.encode())))

    def build(self):
        d = ",".join(str(server) for server in self.servers)
        return self.z(d).hex()


servers = Servers()
servers.add("192.168.0.10", "9339,9449")
# OR
servers.addRange("172.17.64.1", 9006, 9099)

watermark = "Welcome to ZOVserver client!" # or "" , "none" , "null"


file_path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "libsettings.so")
with open(file_path, "w", encoding="utf-8") as f:
    f.write(get_settings(servers.build(), watermark))
