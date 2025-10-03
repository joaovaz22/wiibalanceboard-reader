# 🎮 Wii Balance Board Reader

This repository contains **two C# applications** for recording data from the Wii Balance Board:

- **WiiBalanceReader** – Console version  
- **WiiBalanceReaderGUI** – WinForms GUI version  

Both versions connect to the Wii Balance Board over Bluetooth using [WiimoteLib](https://github.com/BrianPeek/WiimoteLib), stream sensor data, and export it to CSV files for further analysis.

---

## 📂 Repository Structure

```
WiiBalanceBoardReader.sln
├── WiiBalanceReader/        # Console version
│   └── Program.cs
├── WiiBalanceReaderGUI/     # GUI version (WinForms)
│   ├── Form1.cs
│   ├── Form1.Designer.cs
│   └── Program.cs
```

---

## 🚀 Features

- Connects to the Wii Balance Board (RVL-WBC-01) via Bluetooth  
- Tare weight (baseline adjustment)  
- Reset Center of Pressure (CoP)  
- Timed streaming:  
  - **Simple task:** 60 seconds  
  - **Complex task:** 39 seconds  
- CSV logging with timestamps, per-corner loads, total weight, and CoP (X, Y)  

---

## ⚙️ Requirements

- Windows with Bluetooth  
- .NET Framework 4.8 (or later)  
- [WiimoteLib](https://github.com/BrianPeek/WiimoteLib) (DLL reference included)  
- Wii Balance Board (Nintendo RVL-WBC-01)

---

## ▶️ Usage

### Console version
1. Navigate to the console project:
   ```bash
   cd WiiBalanceReader
   dotnet run
   ```
2. Enter participant name and task type:
   ```
   John simple
   Maria complex
   ```
3. Use commands at runtime:
   - `go` → Start streaming  
   - `stop` → Stop streaming  
   - `res` → Reset CoP  
   - `exit` → Quit  

### GUI version
1. Open the solution in Visual Studio  
2. Set **WiiBalanceReaderGUI** as the startup project  
3. Run and use the GUI buttons:  
   - **Connect** – Pair with the board  
   - **Tare** – Set baseline (step off the board first)  
   - **Start** – Begin data collection (60s or 39s)  
   - **Stop** – End streaming  
   - **Reset CoP** – Recenter CoP  
   - **Exit** – Close the application  

---

## 👤 Author

Developed by me, João Vaz, as part of a Master’s Thesis project at Instituto Superior Técnico.

📖 The system was created to study balance performance under different arousal conditions, using haptic biofeedback to modulate physiological state.


---

## 📑 License
This project is released under the MIT License.
