# traffic_generator.py
import requests
import threading
import time

SERVICE_URL = "http://localhost:5000/message"

# Bu thread sürekli istek atarak sistemde yoğunluk oluşturur
def attack_thread(id):
    while True:
        try:
            requests.post(SERVICE_URL, json={"message": f"AtaK{id}", "key": "K"})
            print(f"[Saldırgan {id}] Paket gönderildi.")
        except Exception as e:
            print(f"[Saldırgan {id}] Hata: {e}")
        time.sleep(0.1)  # Yoğunluk için 100ms aralıklarla gönder

# 3 farklı süldirgan threadi oluştur
for i in range(1, 4):
    t = threading.Thread(target=attack_thread, args=(i,), daemon=True)
    t.start()

# Ana thread sonsuz beklemede kalsın
time.sleep(99999)

