# SlimeBalancer

A Unity-based interactive gaming platform that uses a physical balance board controlled by an ESP32 microcontroller. Players control games by tilting a physical board, which reads motion data from an MPU6050 sensor and communicates with the Unity application via Bluetooth.

## 🎮 Features

- **Physical Balance Board Control**: Real-time tilt-based input using MPU6050 accelerometer/gyroscope
- **Bluetooth Communication**: Wireless connection between ESP32 and Unity game client
- **LED Feedback**: WS2812B LED strip with customizable colors and rainbow effects
- **Multiple Mini-Games**:
  - Tilt Game: Control gameplay by tilting the board
  - Snake Game: Navigate using physical board movements
- **Safety Features**:
  - Temperature monitoring with automatic thermal protection
  - Connection status indicators via LED colors
- **Modular Game System**: Easy-to-extend game manager architecture

## 🛠️ Hardware Requirements

### ESP32 Balance Board
- ESP32-WROOM-32 (NodeMCU-32S or similar)
- MPU6050 6-axis accelerometer/gyroscope
- WS2812B LED strip (44 LEDs recommended)
- Bluetooth-enabled ESP32 board

### Wiring Configuration
- **I2C Bus (MPU6050)**:
  - SDA → GPIO 21 (default)
  - SCL → GPIO 22 (default)
- **LED Strip**:
  - Data Pin → GPIO 4
  - Power: 5V, sufficient current for LED count

### Computer/Gaming Device
- Bluetooth capability
- Unity-compatible system (Windows, macOS, or Linux)

## 📋 Software Requirements

### ESP32 Firmware
- PlatformIO IDE or PlatformIO Core
- Platform: Espressif32
- Framework: Arduino
- Libraries:
  - FastLED (^3.10.3)
  - Wire (built-in)
  - BluetoothSerial (built-in)

### Unity Game Client
- Unity 2021.3 LTS or newer recommended
- .NET Framework/Mono
- Input System Package

## 🚀 Setup Instructions

### 1. ESP32 Firmware Setup

1. **Install PlatformIO**:
   - Install [PlatformIO IDE](https://platformio.org/install/ide) or PlatformIO Core
   
2. **Navigate to ESP32 project**:
   ```bash
   cd ESP32
   ```

3. **Build and upload firmware**:
   ```bash
   platformio run --target upload
   ```

4. **Monitor serial output** (optional):
   ```bash
   platformio device monitor
   ```

### 2. Unity Game Client Setup

1. **Open the project**:
   - Launch Unity Hub
   - Add the `SlimeBalancer` folder as a Unity project
   - Open the project with Unity 2021.3 LTS or newer

2. **Configure Bluetooth**:
   - The BluetoothClient script will automatically scan for the "SlimeBalancer" device
   - Ensure your computer's Bluetooth is enabled

3. **Build and run**:
   - Open the main scene from `Assets/Scenes/`
   - Press Play in Unity Editor or build the executable

## 🎯 Usage

### Starting the System

1. **Power on the ESP32 board**:
   - LEDs will turn RED, indicating waiting for connection

2. **Launch the Unity game**:
   - The game will automatically scan for the "SlimeBalancer" Bluetooth device
   - Connection status is displayed in the game UI

3. **Once connected**:
   - LEDs turn TEAL/GREEN (RGB: 48, 213, 150)
   - The board begins sending tilt data to the game

### Playing Games

- **Tilt Controls**: Physical board movements control in-game actions
- **Pitch**: Forward/backward tilt
- **Roll**: Left/right tilt
- **Balance threshold**: ~5 degrees for most games

### LED Control Commands

The Unity client can send LED commands via Bluetooth:

```
"Off>>"                              // Turn off all LEDs
"Rainbow>>"                          // Enable rainbow animation
"Idle>>"                             // Enter sleep mode
"R: 255, G: 0, B: 0, Side: 0>>"     // Set color (Side: 0=all, 1-4=individual sides)
```

Multiple commands can be chained:
```
"R: 255, G: 0, B: 0, Side: 1>>R: 0, G: 255, B: 0, Side: 2>>"
```

## 🔧 Configuration

### ESP32 Settings (in `ESP32/src/main.cpp`)

```cpp
#define LED_PIN 4              // LED strip data pin
#define NUM_LEDS 44            // Number of LEDs in strip
#define BRIGHTNESS 100         // LED brightness (0-255)
```

### MPU6050 Configuration

- **Accelerometer Range**: ±2g
- **Gyroscope Range**: ±500°/s
- **Low-pass Filter**: ~20 Hz
- **Sampling Rate**: ~40 Hz (25ms intervals)

### Temperature Protection

- **Warning Threshold**: 80°C (enters cooldown mode)
- **Resume Threshold**: 60°C (resumes normal operation)

## 📡 Bluetooth Protocol

### Data Format from ESP32 → Unity

```
Mpu_Values: P: <pitch>, R: <roll>, T: <temperature>>>
```

Example:
```
Mpu_Values: P: 12.34, R: -5.67, T: 42.50>>
```

### Commands from Unity → ESP32

- Single or multiple commands separated by `>>`
- RGB values: 0-255
- Side values: 0 (all), 1 (top), 2 (right), 3 (bottom), 4 (left)

## 🔍 Troubleshooting

### ESP32 Not Connecting

1. Check Bluetooth is enabled on your computer
2. Verify the ESP32 is powered and LEDs show RED
3. Check serial monitor output: `platformio device monitor`
4. Ensure Bluetooth device name is "SlimeBalancer"

### Unity Not Detecting Device

1. Check that the BluetoothClient script is active
2. Verify COM port permissions on Windows
3. Try restarting the Unity application
4. Check Windows Device Manager for Bluetooth serial ports

### MPU6050 Not Responding

1. Verify I2C wiring (SDA/SCL connections)
2. Check MPU6050 address (default: 0x68)
3. Ensure proper power supply to MPU6050 (3.3V or 5V depending on module)

### LED Strip Issues

1. Verify data pin connection (GPIO 4)
2. Ensure adequate power supply for LEDs
3. Check LED_TYPE and COLOR_ORDER in firmware match your strip
4. Verify NUM_LEDS matches your physical strip

### Temperature Warning

- If "WARNING: TOO HOT>>" appears, the MPU6050 exceeded 80°C
- System automatically enters cooldown mode
- Normal operation resumes below 60°C
- Ensure adequate ventilation around the sensor

## 🏗️ Project Structure

```
SlimeBalancer/
├── ESP32/                          # ESP32 firmware (PlatformIO)
│   ├── src/
│   │   └── main.cpp               # Main ESP32 firmware
│   ├── platformio.ini             # PlatformIO configuration
│   └── lib/                       # Libraries
│
├── SlimeBalancer/                  # Unity game client
│   ├── Assets/
│   │   ├── Scenes/                # Game scenes
│   │   ├── Scripts/
│   │   │   ├── Bluetooth/         # Bluetooth communication
│   │   │   ├── Games/             # Game implementations
│   │   │   ├── Managers/          # Game managers
│   │   │   └── UI/                # UI scripts
│   │   ├── Prefabs/               # Game prefabs
│   │   ├── Materials/             # 3D materials
│   │   └── Textures/              # Textures and sprites
│   └── ProjectSettings/           # Unity project settings
│
└── README.md                       # This file
```

## 🎓 Development

### Adding New Games

1. Create a new script inheriting from `BaseGame`
2. Override `StartGame()`, `UpdateGame()`, and `EndGame()` methods
3. Access tilt data via `GameManager.InputManager.InputEulerRotation`
4. Register the game in the GameManager

Example:
```csharp
public class MyGame : BaseGame
{
    public override void StartGame()
    {
        base.StartGame();
        // Initialize your game
    }

    public override void UpdateGame()
    {
        base.UpdateGame();
        Vector3 rotation = GameManager.InputManager.InputEulerRotation;
        // Use rotation.x (pitch) and rotation.z (roll)
    }

    public override void EndGame(bool won = false)
    {
        base.EndGame(won);
        // Cleanup
    }
}
```

### Modifying LED Patterns

Edit the `rainbow()` function in `ESP32/src/main.cpp` or add new LED effect functions.

### Adjusting Sensor Sensitivity

Modify the MPU6050 configuration in the `mpuBoot()` function:
- `0x1B` register: Gyroscope range
- `0x1C` register: Accelerometer range
- `0x1A` register: Digital low-pass filter

## 📝 License

This project is developed by Howest.

## 🤝 Contributing

This appears to be an educational project. For contributions or questions, please contact the repository owner.

## 📧 Support

For issues or questions:
1. Check the Troubleshooting section above
2. Review serial monitor output from the ESP32
3. Check Unity console for error messages
4. Open an issue on the GitHub repository

---

**Device Name**: SlimeBalancer  
**Bluetooth Serial**: 115200 baud  
**Platform**: ESP32 + Unity  
**Version**: Educational Project
