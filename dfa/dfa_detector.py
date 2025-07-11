import xml.etree.ElementTree as ET
from scapy.all import sniff, TCP, IP
import time
import subprocess
import requests
from collections import Counter

# === DFA SINIFI ===
class DFA:
    def __init__(self, xml_path):
        self.states = set()
        self.alphabet = set()
        self.transitions = {}
        self.start_state = None
        self.accept_states = set()
        self.load_from_xml(xml_path)

    def load_from_xml(self, path):
        tree = ET.parse(path)
        root = tree.getroot()

        for symbol in root.find("alphabet").findall("symbol"):
            self.alphabet.add(symbol.text)

        for state in root.find("states").findall("state"):
            name = state.attrib["name"]
            self.states.add(name)
            if state.attrib.get("start") == "true":
                self.start_state = name
            if state.attrib.get("accept") == "true":
                self.accept_states.add(name)

        for t in root.find("transitions").findall("transition"):
            from_state = t.find("from").text
            input_symbol = t.find("input").text
            to_state = t.find("to").text
            self.transitions[(from_state, input_symbol)] = to_state

    def next_state(self, current_state, input_symbol):
        return self.transitions.get((current_state, input_symbol), current_state)

    def run_step(self, current_state, input_symbol, tag=""):
        new_state = self.next_state(current_state, input_symbol)
        print(f"[{tag}] {current_state} --({input_symbol})--> {new_state}")
        return new_state, new_state in self.accept_states

# === YOĞUNLUK SEVİYESİ SINIFLANDIRICI ===
def classify_syn_rate(rate):
    if rate <= 10:
        return "low"
    elif rate <= 40:
        return "mid"
    else:
        return "high"

# === SYN PAKETLERİNİ SAY ===
def count_syn_packets(duration=1):
    syn_count = {}

    def packet_handler(packet):
        if packet.haslayer(TCP) and packet[TCP].flags == "S":
            ip = packet[IP].src
            syn_count[ip] = syn_count.get(ip, 0) + 1

    sniff(filter="tcp", prn=packet_handler, timeout=duration)
    return syn_count

# === BAŞLAT ===
if __name__ == "__main__":
    tespit_dfa = DFA("tespit_dfa.xml")
    savunma_dfa = DFA("defense_dfa.xml")

    ip_states = {}
    ip_defense_states = {}

    warned_ips = set()
    banned_ips = set()

    total_packets = 0

    print("🔍 SYN trafiği dinleniyor... (Ctrl+C ile durdur)")

    try:
        while True:
            syn_counts = count_syn_packets(duration=10)
            current_levels = []
            ip_logs = []

            total_packets += sum(syn_counts.values())
            unique_ips = len(syn_counts)
            total_syn = sum(syn_counts.values())
            
            print(f"🌿 DEBUG: Toplam SYN = {total_syn}, Benzersiz IP = {unique_ips}")

            # 🌿 DOĞAL TRAFİK KONTROLÜ
            natural_traffic = total_syn > 100 and unique_ips > 80
            if natural_traffic:
                print("🌿 YÜKSEK AMA DOĞAL TRAFİK TESPİT EDİLDİ – Ban uygulanmayacak bu döngüde.")
                
            for ip, count in syn_counts.items():
                level = classify_syn_rate(count)
                current_levels.append(level)

                # TESPİT DFA
                prev_state = ip_states.get(ip, tespit_dfa.start_state)
                new_state, is_attack = tespit_dfa.run_step(prev_state, level, tag="TESPİT")
                ip_states[ip] = new_state

                # SAVUNMA DFA (sadece doğal trafik değilse banla)
                if not natural_traffic and is_attack:
                    d_prev = ip_defense_states.get(ip, savunma_dfa.start_state)
                    d_new, is_block = savunma_dfa.run_step(d_prev, level, tag="SAVUNMA")
                    ip_defense_states[ip] = d_new

                    if is_block:
                        if ip not in warned_ips:
                            print(f"⚠ IP {ip} SAVUNMA S3 DURUMUNA GİRDİ – Ban süreci başlatılıyor...")
                            warned_ips.add(ip)
                        elif ip not in banned_ips:
                            print(f"🚫 IP {ip} GERÇEK BAN UYGULANIYOR...")
                            subprocess.call(["sudo", "iptables", "-D", "INPUT", "-s", ip, "-j", "DROP"])
                            subprocess.call(["sudo", "iptables", "-A", "INPUT", "-s", ip, "-p", "all", "-j", "DROP"])
                            #subprocess.call(["/usr/sbin/iptables", "-D", "INPUT", "-s", ip, "-j", "DROP"])
                            #subprocess.call(["/usr/sbin/iptables", "-A", "INPUT", "-s", ip, "-p", "all", "-j", "DROP"])
                            banned_ips.add(ip)
                            
                        else:
                            print(f"🚫 IP {ip} HALEN BANLI (SAVUNMA: {d_new})")
                    else:
                        print(f"🛡 IP {ip} izleniyor (SAVUNMA: {d_new})")
                else:
                    print(f"✅ IP {ip} normal trafik (TESPİT: {new_state})")

                ip_logs.append({
                    "ip": ip,
                    "reason": "DDoS Saldırısı" if ip in banned_ips else "Normal",
                    "time": time.strftime("%H:%M:%S")
                })

            # 🌐 GLOBAL SALDIRI TESPİTİ
            q2_count = sum(1 for state in ip_states.values() if state == "q2")
            total_ips = len(ip_states)
            if total_ips > 0:
                q2_ratio = q2_count / total_ips
                if q2_ratio >= 0.3:
                    print(f"🌐 GLOBAL TEHLİKE MODU AKTİF! (%{q2_ratio*100:.0f} IP q2'de)")

            # VERİLERİ GÖNDER (GUI ile uyumlu şekilde)
            try:
                requests.post("http://localhost:5000/update", json={
                    "traffic_level": "high" if "high" in current_levels else "mid" if "mid" in current_levels else "low",
                    "blocked_ips": list(banned_ips),
                    #"ip_nodes": list(ip_states.keys()),
                    "ip_nodes":[ip for ip in ip_states if ip in syn_counts],
                    "ip_category_stats": {
                        "Saldırgan": sum(1 for ip in ip_states if ip_states[ip] == "q2" and ip in banned_ips),
                        "Şüpheli": sum(1 for ip in ip_states if ip_states[ip] == "q1"),
                        "Temiz": sum(1 for ip in ip_states if ip_states[ip] == "q0")
                    },
                    "dfa_detect_state": new_state,
                    "dfa_defense_state": ip_defense_states.get(ip, savunma_dfa.start_state),
                    "packet_rate": total_syn,
                    "packet_total": total_packets,
                    "ip_logs": ip_logs
                })
            except Exception as e:
                print("⚠ Panel verisi gönderilemedi:", e)

            time.sleep(0.5)
    except KeyboardInterrupt:
        print("\n⛔ İzleme durduruldu.")
