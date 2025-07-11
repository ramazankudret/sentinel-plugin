# xor_encrypt.py

def xor_encrypt_decrypt(text, key='K'):
    """
    XOR tabanlı basit bir şifreleme ve çözme fonksiyonu.
    Aynı fonksiyon hem şifreleme hem de çözme işi görür.
    """
    return ''.join(chr(ord(c) ^ ord(key)) for c in text)

# Test örneği
decrypted = "Merhaba GPT"
encrypted = xor_encrypt_decrypt(decrypted)
print("Şifreli:", encrypted)

original = xor_encrypt_decrypt(encrypted)
print("Çözüldü:", original)

