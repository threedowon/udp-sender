[한국어 문서 보기](README_KR.md)

# UDP Multi-Sender

A Windows Forms application for **UDP multi-sending**.  
It allows sending messages to multiple ports simultaneously and monitoring their receiving status.

<img width="795" height="477" alt="Image" src="https://github.com/user-attachments/assets/dd0474f3-a342-4c02-8140-d3d36af927ca" />

## 🚀 Features

- **Multi-port sending**: Send a single message to multiple ports at once
- **Port range support**: Supports individual ports (e.g., `7777`) or port ranges (e.g., `7777-7780`)
- **Real-time logging**: Monitor send/receive status in real time
- **Receive check**: Track receiving status for each port
- **Test data**: Automatically generate and send JSON test data
- **Local IP detection**: Auto-detect your local IP address with the **My IP** button

## 📋 System Requirements

- Windows 10/11
- .NET 6.0 Runtime
- Visual Studio 2022 (for development)

## 🛠️ Installation & Run

### 1. Build from source

```bash
# Clone repository
git clone https://github.com/threedowon/UDPMultiSender.git
cd UDPMultiSender

# Build project
dotnet build

# Run
dotnet run
📖 Usage
1. Server connection setup
Server IP: Enter the target server IP address (default: 127.0.0.1)

Ports: Enter the ports to send to

Single ports: 7777, 7778, 7779

Port range: 7777-7780 (from 7777 to 7780)

Mixed: 7777, 7778-7780, 8000

My IP button: Auto-detect local IP address

Connect button: Connect to server

2. Message sending
Message input: Enter the message to send in the text box

Send button: Send the message to all specified ports

Test Data button: Send JSON-formatted test data

Enter key: Press Enter to quickly send the message

3. Log monitoring
Real-time log: Monitor send/receive status in real time

Receive check: Show receiving status per port

Clear log: Clear all log entries

📊 Test Data Format
The test data is sent in JSON format:

json
코드 복사
{
  "type": "test",
  "timestamp": "2024-01-15 14:30:25.123",
  "message": "UDP test data",
  "randomValue": 456
}
🏗️ Project Structure
bash
코드 복사
UDPMultiSender/
├── MainForm.cs              # Main form logic
├── MainForm.Designer.cs     # UI design
├── Program.cs               # Application entry point
├── UDPTestApp.csproj        # Project file
├── app.ico                  # Application icon
└── README.md                # Project documentation
🛡️ Security Considerations
UDP is a connectionless protocol, so delivery is not guaranteed

Firewall settings may restrict port access

Check your network security policy before use

🐛 Troubleshooting
Connection failed
Verify server IP and ports

Check firewall settings

Ensure network connectivity

No receive confirmation
Ensure target server is listening for UDP packets

Check if the port numbers are correct

Consider potential network latency

Build error
Verify that .NET 6.0 SDK is installed

Check for corrupted project files

📝 License
This project is distributed under the MIT License.
See the LICENSE file for details.

⭐ If this project was helpful, please give it a star!
