"""
from flask import Flask, request, jsonify
from flask_cors import CORS
from xor_encrypt import xor_encrypt_decrypt
import datetime

app = Flask(__name__)
CORS(app)

# Global veriler
traffic_level = "mid"
blocked_ips = []
dfa_detect_state = "q0"
dfa_defense_state = "s0"
ip_category_stats = {"Saldırgan": 0, "Şüpheli": 0, "Temiz": 0}
ip_nodes = []

packet_rate = 0
packet_total = 0
ip_logs = []  # Her kayıt: {"ip": "...", "reason": "...", "time": "..."}

@app.route("/message", methods=["POST"])
def handle_message():
    data = request.get_json()
    message = data.get("message", "")
    key = data.get("key", "K")

    encrypted = xor_encrypt_decrypt(message, key)

    # IP'yi ip_nodes'a ekle (eğer yoksa)
    client_ip = request.remote_addr
    if client_ip not in ip_nodes:
        ip_nodes.append(client_ip)

    # Log'a ekle (örnek kayıt)
    now = datetime.datetime.now().strftime("%H:%M:%S")
    ip_logs.append({"ip": client_ip, "reason": "Normal", "time": now})
    if len(ip_logs) > 50:
        ip_logs.pop(0)

    print(f"[NORMAL CLIENT] Orijinal: {message} | Şifreli: {encrypted}")

    return jsonify({
        "original": message,
        "encrypted": encrypted
    })

@app.route("/traffic", methods=["POST"])
def set_traffic():
    global traffic_level
    level = request.json.get("level")
    if level in ["low", "mid", "high"]:
        traffic_level = level
        return jsonify({"status": "ok", "new_level": traffic_level})
    return jsonify({"status": "error", "message": "Geçersiz trafik seviyesi"}), 400

@app.route("/update", methods=["POST"])
def update_panel_data():
    global traffic_level, blocked_ips, dfa_detect_state, dfa_defense_state, ip_category_stats, ip_nodes
    global packet_rate, packet_total, ip_logs

    data = request.json
    traffic_level = data.get("traffic_level", traffic_level)
    blocked_ips = data.get("blocked_ips", blocked_ips)
    dfa_detect_state = data.get("dfa_detect_state", dfa_detect_state)
    dfa_defense_state = data.get("dfa_defense_state", dfa_defense_state)
    ip_category_stats = data.get("ip_category_stats", ip_category_stats)
    ip_nodes = data.get("ip_nodes", ip_nodes)

    packet_rate = data.get("packet_rate", packet_rate)
    packet_total = data.get("packet_total", packet_total)
    ip_logs = data.get("ip_logs", ip_logs)

    return jsonify({"status": "ok", "message": "Veriler güncellendi"})

@app.route("/status", methods=["GET"])
def get_status():
    return jsonify({
        "traffic_level": traffic_level,
        "blocked_ips": blocked_ips,
        "dfa_detect_state": dfa_detect_state,
        "dfa_defense_state": dfa_defense_state,
        "ip_category_stats": ip_category_stats,
        "ip_nodes": ip_nodes,
        "packet_rate": packet_rate,
        "packet_total": packet_total,
        "ip_logs": ip_logs
    })

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, threaded=True)
"""


from flask import Flask, request, jsonify
from flask_cors import CORS
from xor_encrypt import xor_encrypt_decrypt
import datetime
import subprocess

app = Flask(__name__)
CORS(app)

# Global veriler
traffic_level = "mid"
blocked_ips = []
dfa_detect_state = "q0"
dfa_defense_state = "s0"
ip_category_stats = {"Saldırgan": 0, "Şüpheli": 0, "Temiz": 0}
ip_nodes = []
packet_rate = 0
packet_total = 0
ip_logs = []

# Banlı IP kontrol fonksiyonu
def is_ip_banned(ip):
    try:
        result = subprocess.run(["sudo", "iptables", "-L", "INPUT", "-n"], capture_output=True, text=True)
        return ip in result.stdout
    except Exception as e:
        print(f"HATA: iptables kontrol edilemedi → {e}")
        return False

@app.route("/message", methods=["POST"])
def handle_message():
    data = request.get_json()
    message = data.get("message", "")
    key = data.get("key", "K")
    client_ip = request.remote_addr

    # Banlı IP kontrolü
    if is_ip_banned(client_ip):
        print(f"⛔ [BLOCKED] {client_ip} servis erişimi reddedildi.")
        return jsonify({"error": "IP adresiniz engellenmiştir."}), 403

    encrypted = xor_encrypt_decrypt(message, key)

    # IP node ekle
    if client_ip not in ip_nodes:
        ip_nodes.append(client_ip)

    # Log ekle
    now = datetime.datetime.now().strftime("%H:%M:%S")
    ip_logs.append({"ip": client_ip, "reason": "Normal", "time": now})
    if len(ip_logs) > 50:
        ip_logs.pop(0)

    print(f"[NORMAL CLIENT] Orijinal: {message} | Şifreli: {encrypted}")

    return jsonify({
        "original": message,
        "encrypted": encrypted
    })

@app.route("/traffic", methods=["POST"])
def set_traffic():
    global traffic_level
    level = request.json.get("level")
    if level in ["low", "mid", "high"]:
        traffic_level = level
        return jsonify({"status": "ok", "new_level": traffic_level})
    return jsonify({"status": "error", "message": "Geçersiz trafik seviyesi"}), 400

@app.route("/update", methods=["POST"])
def update_panel_data():
    global traffic_level, blocked_ips, dfa_detect_state, dfa_defense_state, ip_category_stats, ip_nodes
    global packet_rate, packet_total, ip_logs

    data = request.json
    traffic_level = data.get("traffic_level", traffic_level)
    blocked_ips = data.get("blocked_ips", blocked_ips)
    dfa_detect_state = data.get("dfa_detect_state", dfa_detect_state)
    dfa_defense_state = data.get("dfa_defense_state", dfa_defense_state)
    ip_category_stats = data.get("ip_category_stats", ip_category_stats)
    ip_nodes = data.get("ip_nodes", ip_nodes)
    packet_rate = data.get("packet_rate", packet_rate)
    packet_total = data.get("packet_total", packet_total)
    ip_logs = data.get("ip_logs", ip_logs)

    return jsonify({"status": "ok", "message": "Veriler güncellendi"})

@app.route("/status", methods=["GET"])
def get_status():
    return jsonify({
        "traffic_level": traffic_level,
        "blocked_ips": blocked_ips,
        "dfa_detect_state": dfa_detect_state,
        "dfa_defense_state": dfa_defense_state,
        "ip_category_stats": ip_category_stats,
        "ip_nodes": ip_nodes,
        "packet_rate": packet_rate,
        "packet_total": packet_total,
        "ip_logs": ip_logs
    })
    


@app.route("/banned_ips", methods=["GET"])
def get_banned_ips():
    return jsonify(blocked_ips)

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5000, threaded=True)

 
 
 
 
 

