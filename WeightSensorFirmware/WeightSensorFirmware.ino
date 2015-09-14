#include <SoftwareSerial.h>
SoftwareSerial bluetoothSerial(10,11);
String passkey = "1234";
double weightSensorData = 10.0;


String payload;
String receivedMessage;
String messageToSend;

void setup() {
  Serial.begin(9600);
  bluetoothSerial.begin(9600);
}

void loop() {
  // put your main code here, to run repeatedly:
  CheckForBluetoothData();
  
}
void CheckForBluetoothData()
{

  if(bluetoothSerial.available())
  {
      
      char command = bluetoothSerial.read();
      bluetoothSerial.read();
      while(bluetoothSerial.available())
      {
        receivedMessage.concat(bluetoothSerial.read());
      }
      Serial.println(receivedMessage);
      if(command == 'V')
      {
        passkey = receivedMessage;
        payload = String(random(0,15));
        command = 'W';
      }
      else if(command == 'C')
      {
        payload = passkey;
        command = 'V';
      }
      Serial.println(payload);
      if(payload.length() > 0)
      {
        messageToSend.concat((payload.length() + 2));
        messageToSend.concat(command);
        messageToSend.concat(':');
        messageToSend.concat(payload);
        Serial.println(messageToSend);
        bluetoothSerial.print(messageToSend);
      }
      receivedMessage = "";
      messageToSend = "";
      payload = "";

  }

}

