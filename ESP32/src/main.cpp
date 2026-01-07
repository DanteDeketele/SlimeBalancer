#include <Arduino.h>
#include <Wire.h>

const uint8_t MPU_ADDR = 0x68;
int16_t AcX, AcY, AcZ;
int16_t GyX, GyY, GyZ;
int16_t temp;
float pitch, roll, temperature;

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

void readMPU()
{
  uint8_t buf[14];
  mpuReadBytes(0x3B, 14, buf); // vanaf ACCEL_XOUT_H

  Serial.println();
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

  pitch = pitchRad * 180.0 / 3.14159265;
  roll = rollRad * 180.0 / 3.14159265;
  Serial.println("Pitch: " + String(pitch) + " Roll: " + String(roll));

  // Temperatuur in graden Celsius
  temperature = (temp / 340.0) + 36.53;
}

void printBalance()
{
  float threshold = 5.0;

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
  
 if (temperature > 30) {
   Serial.print("It is hot in here! Temperature: " + String(temperature) + "C");
 }
 else {
   Serial.println("It is " + String(temperature) + "C.");
 }
}

void setup()
{
  Serial.begin(115200);
  Wire.begin();

  // Wakker maken
  mpuWrite(0x6B, 0x00); // PWR_MGMT_1 = 0

  // Config voor balansbord:
  // low‑pass filter ~20 Hz, gyro ±500 °/s, accel ±2 g
  mpuWrite(0x1A, 0x04); // CONFIG: DLPF_CFG = 4 → ~21 Hz
  mpuWrite(0x1B, 0x08); // GYRO_CONFIG: FS_SEL = 1 → ±500 °/s
  mpuWrite(0x1C, 0x00); // ACCEL_CONFIG: AFS_SEL = 0 → ±2 g
}

void loop()
{
  readMPU();
  computeAngles();
  printBalance();
  delay(20); // ~50 Hz
}