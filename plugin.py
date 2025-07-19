import sys
import json
import time
from dfa import dfa_detector

LOG_FILE = "sentinel_log.txt"

def log(message):
    with open(LOG_FILE, "a") as f:
        f.write(f"[{time.ctime()}] {message}\n")

class SentinelPlugin:
    def __init__(self):
        self.handlers = {}

    def add_command(self, command, handler):
        self.handlers[command] = handler

    def run(self):
        log("Plugin started. Listening for input...")
        while True:
            line = sys.stdin.readline()
            if not line:
                log("No input received. Exiting.")
                break
            log(f"Received input: {line.strip()}")
            try:
                request = json.loads(line)
                command = request.get("function", "").lower()
                if command in self.handlers:
                    log(f"Dispatching to handler: {command}")
                    self.handlers[command](request)
                else:
                    self.send_failure(f"Unknown command: {command}")
            except Exception as e:
                self.send_failure(f"Error parsing input: {str(e)}")
                log(f"Exception: {str(e)}")

    def send_success(self, data):
        response = json.dumps({"success": True, "result": data}) + "\n"
        sys.stdout.write(response)
        sys.stdout.flush()
        log(f"Sent success response: {response.strip()}")

    def send_failure(self, message):
        response = json.dumps({"success": False, "error": message}) + "\n"
        sys.stdout.write(response)
        sys.stdout.flush()
        log(f"Sent failure response: {response.strip()}")

def dfa_check(request):
    parameters = request.get("parameters", {})
    ip = parameters.get("ip", "127.0.0.1")
    syn_count = parameters.get("syn_count", 0)

    log(f"DFA check triggered for IP {ip} with {syn_count} SYNs")
    result = dfa_detector.detect_attack(ip, syn_count)

    response = {
        "success": True,
        "result": result
    }

    sys.stdout.write(json.dumps(response) + "\n")
    sys.stdout.flush()
    log(f"DFA result sent: {json.dumps(result)}")

if __name__ == "__main__":
    plugin = SentinelPlugin()
    plugin.add_command("dfa_check", dfa_check)
    plugin.run()
