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
#define LED_TYPE WS2812B
#define COLOR_ORDER GRB
byte r, g, b, side;

CRGB leds[NUM_LEDS];

uint8_t startColor = 0;
const uint8_t colorStep = 6;
unsigned long previousTimeLEDs = 0;
const unsigned long timeStepLEDs = 50;
unsigned long previousTimeBT = 0;
const unsigned long timeStepBT = 25;
bool rainbowMode = false;

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

void rainbow()
{
  Serial.println("Starting rainbow mode...");
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
  mpuWrite(0x6B, 0x40); // mpu in slaapstand houden tot verbinding

  FastLED.addLeds<LED_TYPE, LED_PIN, COLOR_ORDER>(leds, NUM_LEDS);
  FastLED.setBrightness(BRIGHTNESS);
  fill_solid(leds, NUM_LEDS, CRGB::Red); // Initialize all LEDs to off
  FastLED.show();
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
      connected = true;
      fill_solid(leds, NUM_LEDS, CRGB(48, 213, 150)); // Turn on all LEDs to indicate connection
      FastLED.show();
      mpuBoot();
    }
    // doe je loops hier
    if (!toHot)
    {
      unsigned long currentTime = millis();
      if (currentTime - previousTimeBT >= timeStepBT)
      {
        previousTimeBT = currentTime;
        readMPU();
        computeAngles();
        // printBalance(5.0); // drempel van 5 graden
        sendAngles();
        if (temperature >= 80.0)
        {
          toHot = true;
          Serial.println(">> MPU too hot! Entering cooldown mode.");
          SerialBT.println("WARNING: TO HOT>>");
          fill_solid(leds, NUM_LEDS, CRGB::Black); // Indicate overheating
          FastLED.show();
        }
      }
      if (SerialBT.available())
      {
        rainbowMode = false;
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
        while (endIndex != -1 && partIndex < 10)
        {
          incomingParts[partIndex] = incoming.substring(startIndex, endIndex);
          partIndex++;
          startIndex = endIndex + 2;
          endIndex = incoming.indexOf(">>", startIndex);
        }

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
            fill_solid(leds, NUM_LEDS, CRGB::Black);
            FastLED.show();
            mpuWrite(0x6B, 0x40); // mpu in slaapstand
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
      if (rainbowMode)
      {
        rainbow();
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
      mpuWrite(0x6B, 0x40);                  // mpu in slaapstand van zodra de verbinding wegvalt
      fill_solid(leds, NUM_LEDS, CRGB::Red); // Turn off all LEDs
      FastLED.show();
    }
    Serial.println("Waiting for client to connect...");

    delay(1000);
  }
}
