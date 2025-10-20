# 🧩 IBM MQ Connector (.NET)

A lightweight **C# console utility** for connecting to **IBM MQ**, sending and receiving messages, and processing text files through MQ queues with optional SSL support.

---

## 🚀 Features

- ✅ Connects to **IBM MQ Queue Managers** using client or managed transport  
- 🔒 Supports **SSL/TLS** with configurable cipher suites and key repositories  
- 📤 Put & Get messages interactively via console  
- 📁 Automatically send text files from a folder to MQ  
- 🗃️ Organizes processed files into **BACKUP** and **UNPROCESSED** folders  
- 💬 Detailed console logging with timestamps and colors  
- 🌀 Animated startup splash screen  

---

## 🧱 Project Structure

IBMMQConnect/
├── Program.cs              # Main application logic
├── mqsettings.json         # Configuration file
├── bin/                    # Compiled binaries
└── README.md               # Documentation

---

## ⚙️ Configuration (`mqsettings.json`)

Example configuration file:

{
  "QueueManager": "QM1",
  "QueueName": "DEV.QUEUE.1",
  "Host": "192.168.0.100",
  "Port": 1414,
  "Channel": "DEV.APP.SVRCONN",
  "UserId": "app",
  "Password": "passw0rd",
  "UseSSL": true,
  "SSLKeyRepository": "/var/mqm/ssl/key.kdb",
  "SSLCipherSpec": "TLS_RSA_WITH_AES_128_CBC_SHA256",
  "SSLPeerName": "",
  "SSLCertRevocationCheck": false,
  "MQTransportProperty": "TRANSPORT_MQSERIES_CLIENT"
}

---

## 🖥️ Usage

### 🧩 Build & Run

dotnet build
dotnet run

---

### 🎯 Menu Options

| Option | Description |
|--------|--------------|
| 1 | Put a message — Send a message manually to the queue |
| 2 | Get all messages — Retrieve and display messages from the queue |
| 3 | Put & Get — Send a message and immediately read messages |
| 4 | Send files — Read text files from a folder and send to queue |
| 5 | Exit — Close the MQ connection |

---

## 📂 File Processing

When using **Option 4 (Send files)**:
- You’ll be prompted for a folder path and file extension.  
- Each file is sent to the MQ queue as text.  
- Successfully sent files → moved to BACKUP  
- Failed files → moved to UNPROCESSED

---

## 🧰 Dependencies

- .NET 6.0+
- IBM MQ Client for .NET
- Newtonsoft.Json

---

## 🧩 Logging

Each log entry includes:
- Timestamp
- Log level indicator ([✓], [!], [ERROR])
- Color-coded console output

Example:
[2025-10-20 11:42:AM] Step 4: Connecting to IBM MQ...
[2025-10-20 11:42:AM] [✓] Connected to Queue Manager: QM1

---

## 🔐 SSL Configuration Tips

If you’re using SSL:
- Copy .kdb, .sth, and .rdb files into your SSL directory (e.g., /var/mqm/ssl/)
- Ensure MQ channel is configured with the same cipher spec

Example setup on Linux:

runmqakm -keydb -create -db key.kdb -pw 12345 -type cms -stash
runmqakm -cert -add -db key.kdb -pw 12345 -label ibmwebspheremqadmin -file admin.arm -format ascii -trust enable

---

## 🧾 License

This project is licensed under the MIT License.

