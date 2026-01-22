#include <Arduino.h>
#include <Wire.h>
#include <BluetoothSerial.h>
#include <FastLED.h>

// Check to ensure Bluetooth is enabled in the ESP32 settings
#if !defined(CONFIG_BT_ENABLED) || !defined(CONFIG_BLUEDROID_ENABLED)
#error Bluetooth is not enabled! Please run 'make menuconfig' and enable it.
#endif

// variabelen voor Bluetooth
BluetoothSerial SerialBT;
bool connected = false;

// variabelen voor MPU6050
const uint8_t MPU_ADDR = 0x68;
int16_t AcX, AcY, AcZ;
int16_t GyX, GyY, GyZ;
int16_t temp;
float pitch, roll, temperature;
bool toHot = false;

// variabelen voor ledstrip
#define LED_PIN 4
#define NUM_LEDS 44
#define BRIGHTNESS 100
#define BRIGHTNESS_STANDBY 100
#define LED_TYPE WS2812B
#define COLOR_ORDER GRB
byte r, g, b, side;

CRGB leds[NUM_LEDS];

uint8_t startColor = 0;
const uint8_t colorStep = 6;

// overige variabelen
#define BATTERIJ_PIN 27
unsigned long previousTimeLEDs, previousTimeBT, previousTimeBTDisconnect, previousTimeStandBy, previousTimeIdle = millis();
const unsigned long timeStepLEDs = 40;
const unsigned long timeStepBT = 50;
const unsigned long timeStepBTDisconnect = 60000; // 1 minuut
const unsigned long timeStepStandBy = 1000;       // 1 seconde
const unsigned long timeStepIdle = 500;
bool rainbowMode = false;
bool runlightMode = false;
bool standbyMode = false;
byte ledplace_counter = NUM_LEDS;
byte previousBatteryPercentage = -1;

void mpuWrite(uint8_t reg, uint8_t val)
{
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(reg);
  Wire.write(val);
  Wire.endTransmission(true);
}

void mpuReadBytes(uint8_t reg, uint8_t count, uint8_t *dest)
{
  Wire.beginTransmission(MPU_ADDR);
  Wire.write(reg);
  Wire.endTransmission(false);
  Wire.requestFrom(MPU_ADDR, count);
  for (uint8_t i = 0; i < count; i++)
  {
    dest[i] = Wire.read();
  }
}

void mpuBoot()
{
  mpuWrite(0x6B, 0x00); // mpu Wakker maken

  // Config voor balansbord:
  // low‑pass filter ~20 Hz, gyro ±500 °/s, accel ±2 g
  mpuWrite(0x1A, 0x04); // CONFIG: DLPF_CFG = 4 → ~21 Hz
  mpuWrite(0x1B, 0x08); // GYRO_CONFIG: FS_SEL = 1 → ±500 °/s
  mpuWrite(0x1C, 0x00); // ACCEL_CONFIG: AFS_SEL = 0 → ±2 g
}

void readMPU()
{
  uint8_t buf[14];
  mpuReadBytes(0x3B, 14, buf); // vanaf ACCEL_XOUT_H

  // Serial.println();
  AcX = (buf[0] << 8) | buf[1];
  AcY = (buf[2] << 8) | buf[3];
  AcZ = (buf[4] << 8) | buf[5];
  temp = (buf[6] << 8) | buf[7];
  GyX = (buf[8] << 8) | buf[9];
  GyY = (buf[10] << 8) | buf[11];
  GyZ = (buf[12] << 8) | buf[13];
}

void computeAngles()
{
  // omzetting naar g bij ±2g range: gevoeligheid ~16384 LSB/g
  float ax = AcX / 16384.0;
  float ay = AcY / 16384.0;
  float az = AcZ / 16384.0;

  // Hoeken uit accel (in radialen)
  float pitchRad = atan2(ax, sqrt(ay * ay + az * az));
  float rollRad = atan2(ay, sqrt(ax * ax + az * az));

  // Omzetting naar graden
  pitch = pitchRad * 180.0 / 3.14159265;
  roll = rollRad * 180.0 / 3.14159265;
  // Serial.println("Pitch: " + String(pitch) + " Roll: " + String(roll));

  // Temperatuur in graden Celsius
  temperature = (temp / 340.0) + 36.53;
}

void printBalance(float threshold)
{
  if (fabs(pitch) < threshold && fabs(roll) < threshold)
  {
    Serial.println("BALANCED");
  }
  else if (pitch >= threshold)
  {
    Serial.println("FORWARD");
  }
  else if (pitch <= -threshold)
  {
    Serial.println("BACKWARD");
  }
  else if (roll >= threshold)
  {
    Serial.println("RIGHT");
  }
  else if (roll <= -threshold)
  {
    Serial.println("LEFT");
  }
}

void sendAngles()
{
  SerialBT.print("Mpu_Values: P: ");
  SerialBT.print(pitch);
  SerialBT.print(", R: ");
  SerialBT.print(roll);
  SerialBT.print(", T: ");
  SerialBT.print(temperature);
  SerialBT.println(">>");
}

void readBattery()
{
  int batteryLevel = analogRead(BATTERIJ_PIN);
  // Serial.print("Raw Battery Level: ");
  // Serial.println(batteryLevel);
  if (batteryLevel < 3102)
  {
    SerialBT.println("WARNING: LOW BATTERY>>");
    // Serial.println("WARNING: LOW BATTERY>>");
  }
  else if (batteryLevel > 4095)
  {
    // foutieve waarde, negeren
  }
  else
  {
    byte batteryPercentage = map(batteryLevel, 3102, 4095, 0, 100); // omzetten naar percentage

    SerialBT.print("Battery_Level: ");
    SerialBT.print(batteryPercentage);
    SerialBT.println(">>");

    // Serial.print("Battery_Level: ");
    // Serial.print(batteryPercentage);
    // Serial.println(">>");
  }

  // Serial.println("Battery level: " + String(batteryLevel));
}

void readAndSendMPU()
{
  readMPU();
  computeAngles();
  // printBalance(5.0); // drempel van 5 graden
  sendAngles();
  readBattery();
  if (temperature >= 80.0)
  {
    toHot = true;
    Serial.println(">> MPU too hot! Entering cooldown mode.");
    SerialBT.println("WARNING: TO HOT>>");
    fill_solid(leds, NUM_LEDS, CRGB::Black); // Indicate overheating
    FastLED.show();
  }
}

void rainbow()
{
  unsigned long currentTime = millis();
  if (currentTime - previousTimeLEDs >= timeStepLEDs)
  {
    previousTimeLEDs = currentTime;

    // Shift all LEDs one step to the right
    for (int i = NUM_LEDS - 1; i > 0; i--)
    {
      leds[i] = leds[i - 1];
    }

    // Insert new hue at the front
    leds[0] = CHSV(startColor, 255, 255);

    // Advance to the next hue
    startColor += colorStep;
    if (startColor > 255)
    {
      startColor = 0;
    }

    FastLED.show();
  }
}

void runlight(const struct CRGB &color)
{
  unsigned long currentTime = millis();
  if (currentTime - previousTimeLEDs >= timeStepLEDs)
  {
    previousTimeLEDs = currentTime;

    // Shift all LEDs one step to the right
    for (int i = NUM_LEDS - 1; i > 0; i--)
    {
      leds[i] = leds[i - 1];
    }

    // Insert the specified color at the front
    if (ledplace_counter >= NUM_LEDS)
    {
      ledplace_counter = 0;
      leds[0] = color;
    }
    else
    {
      leds[0] = CRGB::Black;
      ledplace_counter++;
    }
    FastLED.show();
  }
}

void setup()
{
  Serial.begin(115200);
  while (!Serial)
    ;
  // Bluetooth initialisatie
  SerialBT.begin("SlimeBalancer");

  Wire.begin();
  mpuBoot();

  FastLED.addLeds<LED_TYPE, LED_PIN, COLOR_ORDER>(leds, NUM_LEDS);
  FastLED.setBrightness(BRIGHTNESS);
  fill_solid(leds, NUM_LEDS, CRGB::Red);
  FastLED.show();
  pinMode(BATTERIJ_PIN, INPUT);
}

void loop()
{
  // Controleer Bluetooth-verbinding
  if (SerialBT.hasClient())
  {
    // start alles op als de verbinding net gemaakt is
    if (!connected)
    {
      Serial.println(">> Client connected!");
      // mpuBoot();
      connected = true;
      standbyMode = false;
      fill_solid(leds, NUM_LEDS, CRGB(48, 213, 150)); // Turn on all LEDs to indicate connection
      rainbowMode = true;
      FastLED.setBrightness(BRIGHTNESS);
      FastLED.show();
      runlightMode = false;
    }
    // doe je loops hier
    if (!toHot)
    {
      unsigned long currentTime = millis();
      if ((currentTime - previousTimeBT >= timeStepBT) && !runlightMode)
      {
        previousTimeBT = currentTime;
        readAndSendMPU();
      }
      if (SerialBT.available())
      {
        String incoming = SerialBT.readStringUntil('\n');
        Serial.println("Received via BT: " + incoming);
        // vb incoming: "R: 255, G: 127, B: 0, Side: 1>>"
        // vb2: "Off>>"
        // vb3: "Rainbow>>"
        // vb4: "Idle>>"
        // vb5: "R: 0, G: 0, B: 255, Side: 0>>Raindbow>>Idle>>R: 255, G: 255, B: 255, Side: 2>>R: 0, G: 255, B: 0, Side: 3>>"

        // split de string op de plaats van >> en steek de verschillende delen in een array (zie vb 5)
        String incomingParts[10]; // max 10 commands per keer
        int partIndex = 0;
        int startIndex = 0;
        int endIndex = incoming.indexOf(">>");

        if ((endIndex != -1) && (endIndex < 25) && (incoming.length() > 4))
        {
          while (endIndex != -1 && partIndex < 10)
          {
            incomingParts[partIndex] = incoming.substring(startIndex, endIndex);
            partIndex++;
            startIndex = endIndex + 2;
            endIndex = incoming.indexOf(">>", startIndex);
          }
          rainbowMode = false;
          runlightMode = false;
          FastLED.setBrightness(BRIGHTNESS);

          for (int i = 0; i < partIndex; i++)
          {
            if (incomingParts[i].startsWith("Off"))
            {
              fill_solid(leds, NUM_LEDS, CRGB::Black); // Turn off all LEDs
              FastLED.show();
            }
            else if (incomingParts[i].startsWith("Rainbow"))
            {
              rainbowMode = true;
            }
            else if (incomingParts[i].startsWith("Idle"))
            {
              runlightMode = true;
              ledplace_counter = NUM_LEDS;
              fill_solid(leds, NUM_LEDS, CRGB::Black);
              // leds[0] = CRGB(48, 213, 150);
              FastLED.setBrightness(BRIGHTNESS_STANDBY);
              FastLED.show();
            }
            else
            {
              int rIndex = incomingParts[i].indexOf("R: ");
              int gIndex = incomingParts[i].indexOf("G: ");
              int bIndex = incomingParts[i].indexOf("B: ");
              int sideIndex = incomingParts[i].indexOf("Side: ");

              if (rIndex != -1 && gIndex != -1 && bIndex != -1 && sideIndex != -1)
              {
                r = incomingParts[i].substring(rIndex + 3, incomingParts[i].indexOf(",", rIndex)).toInt();
                g = incomingParts[i].substring(gIndex + 3, incomingParts[i].indexOf(",", gIndex)).toInt();
                b = incomingParts[i].substring(bIndex + 3, incomingParts[i].indexOf(",", bIndex)).toInt();
                side = incomingParts[i].substring(sideIndex + 6).toInt();

                Serial.println("Parsed values - R: " + String(r) + " G: " + String(g) + " B: " + String(b) + " Side: " + String(side));

                // Update LED colors based on side
                if (side == 0) // all sides
                {
                  fill_solid(leds, NUM_LEDS, CRGB(r, g, b));
                }
                else
                {
                  for (int i = (NUM_LEDS * (side - 1)) / 4; i < (NUM_LEDS * side) / 4; i++)
                  {
                    leds[i] = CRGB(r, g, b);
                  }
                }
                FastLED.show();
              }
            }
          }
        }
      }
      if (rainbowMode)
      {
        rainbow();
      }
      else if (runlightMode)
      {
        // Serial.println("Idle mode");
        unsigned long currentTime = millis();
        if (currentTime - previousTimeIdle >= timeStepIdle)
        {
          // Serial.println("Idle mode action");
          previousTimeIdle = currentTime;
          readAndSendMPU();
          runlight(CRGB(48, 213, 150));
        }
      }
    }
    else
    {
      // MPU is te warm, wachten tot hij afgekoeld is
      readMPU();
      computeAngles();
      if (temperature < 60.0)
      {
        toHot = false;
        Serial.println("<< MPU cooled down. Resuming normal operation.");
        SerialBT.println("WARNING: COOLED>>");
      }
      delay(1000);
    }
  }

  // Geen verbinding, wachten op verbinding
  else
  {
    if (connected)
    {
      Serial.println("<< Client disconnected.");
      connected = false;
      // mpuWrite(0x6B, 0x40); // mpu in slaapstand van zodra de verbinding wegvalt
      fill_solid(leds, NUM_LEDS, CRGB::Red);
      FastLED.show();
      previousTimeBTDisconnect = millis();
    }

    if (standbyMode)
    {
      unsigned long currentTime = millis();
      if ((currentTime - previousTimeStandBy >= timeStepStandBy))
      {
        previousTimeStandBy = currentTime;
        // Serial.println("Waiting for client to connect...");
        if (!runlightMode)
        {
          runlightMode = true;
          ledplace_counter = NUM_LEDS;
          fill_solid(leds, NUM_LEDS, CRGB::Black);
          // leds[0] = CRGB::Red;
          FastLED.setBrightness(BRIGHTNESS_STANDBY);
          FastLED.show();
        }
        else
        {
          runlight(CRGB::Red);
        }
      }
    }
    else
    {
      unsigned long currentTime = millis();
      if (currentTime - previousTimeBTDisconnect >= timeStepBTDisconnect)
      {
        standbyMode = true;
        previousTimeBTDisconnect = currentTime;
      }
    }
  }
}
