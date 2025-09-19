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
