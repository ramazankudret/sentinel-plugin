import sys
import json
from dfa import dfa_detector

class SentinelPlugin:
    def __init__(self):
        self.handlers = {}

    def add_command(self, command, handler):
        self.handlers[command] = handler

    def run(self):
        while True:
            line = sys.stdin.readline()
            if not line:
                break
            try:
                request = json.loads(line)
                command = request.get("function", "").lower()
                if command in self.handlers:
                    self.handlers[command](request)
                else:
                    self.send_failure(f"Unknown command: {command}")
            except Exception as e:
                self.send_failure(f"Error parsing input: {str(e)}")

    def send_success(self, data):
        sys.stdout.write(json.dumps({"success": True, "result": data}) + "\n")
        sys.stdout.flush()

    def send_failure(self, message):
        sys.stdout.write(json.dumps({"success": False, "error": message}) + "\n")
        sys.stdout.flush()

def dfa_check(request):
    parameters = request.get("parameters", {})
    ip = parameters.get("ip", "127.0.0.1")
    syn_count = parameters.get("syn_count", 0)

    result = dfa_detector.detect_attack(ip, syn_count)

    # pipe response
    sys.stdout.write(json.dumps({
        "success": True,
        "result": result
    }) + "\n")
    sys.stdout.flush()

if __name__ == "__main__":
    plugin = SentinelPlugin()
    plugin.add_command("dfa_check", dfa_check)
    plugin.run()
