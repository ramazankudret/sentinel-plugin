# normal_client.py
import requests
import time

SERVICE_URL = "http://localhost:5000/message"

while True:
    try:
        response = requests.post(SERVICE_URL, json={"message": "Merhaba", "key": "K"})
        if response.status_code == 200:
            try:
                data = response.json()
                original = data.get("original", "N/A")
                encrypted = data.get("encrypted", "N/A")
                print(f"[NORMAL CLIENT] Orijinal: '{original}' | Şifreli: '{encrypted}'")
            except Exception as parse_err:
                print(f"[NORMAL CLIENT] JSON parse hatası: {parse_err}")
        else:
            print(f"[NORMAL CLIENT] Sunucu cevap veremedi. Kod: {response.status_code}")
    except Exception as e:
        print(f"[NORMAL CLIENT] Hata: '{e}'")
    
    time.sleep(2)  # 2 saniyede bir istek at
