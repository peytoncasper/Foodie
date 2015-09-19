
int fsrPin = A0;     // the FSR and 10K pulldown are connected to a0
int fsrReading;     // the analog reading from the FSR resistor divider
int fsrVoltage;     // the analog reading converted to voltage
unsigned long fsrResistance;  // The voltage converted to resistance, can be very big so make "long"
unsigned long fsrConductance; 
double fsrForce;       // Finally, the resistance converted to force
 

String payload;
String receivedMessage;
String messageToSend;

void setup() {
  Serial.begin(9600);
  Serial2.begin(9600);

}

void loop() {
//  CheckForBluetoothData();
Serial.print(String(getWeight()) + " Grams");
delay(1000);
//  while(Serial2.available())
//  {
//    Serial.print(Serial2.read());
//  }
//  Serial2.print("Hy");
}
void CheckForBluetoothData() 
 { 
 
 
   if(Serial2.available()) 
   { 
        
       char command = Serial2.read(); 
//       Serial2.read(); 
//       while(Serial2.available()) 
//       { 
//         receivedMessage.concat(Serial2.read()); 
//       } 

 
 
       if(command == 'W') 
       { 
         payload = String(getWeight()); 
         command = 'W'; 
         Serial.println(payload);
       } 

       if(payload.length() > 0) 
       { 
         messageToSend.concat((payload.length() + 2)); 
         messageToSend.concat(command); 
         messageToSend.concat(':'); 
         messageToSend.concat(payload); 
         Serial.println(messageToSend); 
         Serial2.print(messageToSend); 
       } 
       receivedMessage = ""; 
       messageToSend = ""; 
       payload = ""; 

 
   } 
 
 
} 


double getWeight()
{
  fsrReading = analogRead(fsrPin);  
  Serial.print("Analog reading = ");
  Serial.println(fsrReading);
 
  // analog voltage reading ranges from about 0 to 1023 which maps to 0V to 5V (= 5000mV)
  fsrVoltage = map(fsrReading, 0, 1023, 0, 3300);
  Serial.print("Voltage reading in mV = ");
  Serial.println(fsrVoltage);  
 
  if (fsrVoltage == 0) {
    Serial.println("No pressure");  
  } else {
    // The voltage = Vcc * R / (R + FSR) where R = 10K and Vcc = 5V
    // so FSR = ((Vcc - V) * R) / V        yay math!
    fsrResistance = 3300 - fsrVoltage;     // fsrVoltage is in millivolts so 5V = 5000mV
    fsrResistance *= 10000;                // 10K resistor
    fsrResistance /= fsrVoltage;
    Serial.print("FSR resistance in ohms = ");
    Serial.println(fsrResistance);
 
    fsrConductance = 1000000.0;           // we measure in micromhos so 
    fsrConductance /= fsrResistance;
    Serial.print("Conductance in microMhos: ");
    Serial.println(fsrConductance);
 
    // Use the two FSR guide graphs to approximate the force
    if (fsrConductance <= 1000) {
      fsrForce = (fsrConductance / 80.0);

      Serial.print("Force in Newtons: ");
      Serial.println(fsrForce,2);      
    } else {
      fsrForce = fsrConductance - 1000;
      fsrForce /= 30.0;
      Serial.print("Force in Newtons: ");
      Serial.println(fsrForce,2);            
    }
  }
  Serial.println("--------------------");
  
  // Force to Mass Conversion
  // 1kg of mass weights 9.81 newtons (Gravitational Force)on Earth
  // 1 Newton = .102 kg (About)
  // fsrForce (Newtons) * .102 (Kg) = Kg
  // 1000 Grams = 1 Kg, 102 Grams in a Newton
  // fsrForce (Newtons) * 102 = Grams
  return fsrForce * 102;
}


