# Sentinel Plugin for NVIDIA Project G-Assist

## Overview

Sentinel Plugin is a custom G-Assist plugin designed to detect and defend against DDoS attacks in real-time.  
It leverages deterministic finite automata (DFA) to analyze network traffic patterns and classify IP addresses based on attack likelihood.  
This plugin integrates seamlessly with NVIDIA’s Project G-Assist, providing automated threat detection and mitigation directly on your system.

## Features

- Real-time SYN packet monitoring
- DFA-based attack detection and defense mechanism
- IP classification: normal, suspicious, attacker
- Automated IP banning via firewall rules (iptables)
- JSON API integration for Windows Forms GUI panel
- Local processing with no cloud dependency

## Requirements

- NVIDIA GeForce RTX GPU (recommended for G-Assist compatibility, minimum 12 GB VRAM)
- Python 3.8 or higher
- Dependencies listed in `requirements.txt`
- Linux environment for `iptables` ban commands (Windows requires adaptation for firewall rules)
- Admin/root permissions for network packet capture
- `tespit_dfa.xml` and `defense_dfa.xml` in `/dfa` directory
- Windows system for the custom GUI panel (C# Windows Forms app)

## Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/ramazankudret/sentinel-plugin.git
   cd sentinel-plugin


2. Install Python dependencies:

pip install -r requirements.txt

3. Make sure XML DFA files are in the /dfa folder.
4. Build the Windows Forms panel using Visual Studio or your preferred IDE.

## Usage

1. Running the Plugin
- The plugin listens for JSON commands on standard input (stdin).
- Use the dfa_check command with parameters ip and syn_count to evaluate an IP’s status.
- It returns JSON responses for real-time monitoring.

Example JSON command:


{
  "function": "dfa_check",
  "parameters": 
  {
    "ip": "192.168.1.5",
    "syn_count": 42
  }
}

2. Running the Windows Forms Panel
- The Windows Forms GUI visualizes real-time network traffic data:
- Shows traffic levels (low, mid, high)
- Displays total packets, unique IPs
- Lists blocked IPs
- Visualizes IP states (clean, suspicious, attacker)

## How it works:

- dfa_detector.py sends JSON data to a local HTTP endpoint or IPC that your Windows Forms app listens to.

- The GUI updates charts, lists, and nodes automatically.


## How to use the panel:

1. Build and run the Windows Forms application.

2. Run dfa_detector.py — it automatically pushes data to the panel.

3. Watch live traffic updates on your dashboard.

## How It Works:

1. Packet Monitoring: Captures SYN packets to monitor connection requests.

2. DFA Detection: Uses tespit_dfa.xml to classify each IP’s traffic level.

3. DFA Defense: Uses defense_dfa.xml to decide when to block IPs.

4. Ban Action: Automatically executes iptables rules (on Linux).

5. Panel Reporting: Continuously sends JSON data to the Windows Forms GUI for live visualization.


## Limitations:

- Optimized for Linux for packet sniffing and banning; Windows firewall support may need custom adaptation.

- Admin/root privileges required.

- The plugin was tested locally, but could not be tested inside the G-Assist runtime due to a GPU VRAM limit (8 GB vs. 12 GB requirement).



## Important Notes:

Due to hardware constraints, the plugin could not be tested in the actual G-Assist environment because our local GPU has only 8 GB VRAM (minimum 12 GB required).However, the core DFA detection and C# panel integration were tested extensively under local conditions.In our limited test environment, we simulated SYN traffic using local synthetic traffic generators and monitored real-time state transitions through the DFA logic.Firewall ban commands were verified using Linux `iptables` rules, and the Windows Forms panel successfully displayed live packet statistics, blocked IPs, and state changes.We ensured the JSON message flow works properly by manually invoking the `plugin.py` with example stdin commands that reflect the expected G-Assist function calls.All traffic data and labels in the panel are currently in Turkish, since the system was developed for Turkish users.We chose not to translate yet to avoid functional mismatches. English localization can be added later if needed.Voice command integration: The plugin is fully compatible with G-Assist’s function call system. Voice commands are optional and can be added in the manifest later.We kindly ask for your understanding of these hardware and localization constraints.Despite the limitations, we believe this unique approach to local, GPU-accelerated DDoS detection showcases the potential for advanced security plugins within Project G-Assist.


## Contributing:
Contributions and feedback are welcome! Feel free to open issues or submit pull requests.

## License:
Apache 2.0 License

Thank you for your support and understanding!

- E-mail: rmznkudret06@gmail.com
