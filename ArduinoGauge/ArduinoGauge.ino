/*
 Name:		ArduinoGauge.ino
 Created:	12/22/2023 2:14:17 AM
 Author:	Joshua Joesten
*/

#include <SwitecX25.h>
#include <CmdMessenger.h>

#define STEPS (315*3)
#define NUM_MOTORS 12

SwitecX25 motor0(STEPS, 2, 3, 4, 5);
SwitecX25 motor1(STEPS, 6, 7, 8, 9);
SwitecX25 motor2(STEPS, 10, 11, 12, 13);
SwitecX25 motor3(STEPS, 14, 15, 16, 17);
SwitecX25 motor4(STEPS, 18, 19, 20, 21);
SwitecX25 motor5(STEPS, 22, 23, 24, 25);
SwitecX25 motor6(STEPS, 26, 27, 28, 29);
SwitecX25 motor7(STEPS, 30, 31, 32, 33);
SwitecX25 motor8(STEPS, 34, 35, 36, 37);
SwitecX25 motor9(STEPS, 38, 39, 40, 41);
SwitecX25 motor10(STEPS, 42, 43, 44, 45);
SwitecX25 motor11(STEPS, 46, 47, 48, 49);
SwitecX25 motors[NUM_MOTORS] = { motor0, motor1, motor2, motor3,motor4,motor5,motor6,motor7,motor8,motor9,motor10,motor11 };

CmdMessenger cmdMessenger = CmdMessenger(Serial);

// List of recognized CmdMessenger commands
enum {
	kHandshakeRequest,
	kHandshakeResponse,
	kSetTarget,
	kStatus,
	kZero,
	kZeroAll,
};

void attachCommandCallbacks() {
	cmdMessenger.attach(onUnknownCallback);
	cmdMessenger.attach(kHandshakeRequest, onHandshake);
	cmdMessenger.attach(kSetTarget, onSetTarget);
	cmdMessenger.attach(kZero, onZero);
	cmdMessenger.attach(kZeroAll, onZeroAll);
}

void onUnknownCallback() {
	cmdMessenger.sendCmd(kStatus, "Unknown command received.");
}

void onHandshake() {
	cmdMessenger.sendCmdStart(kHandshakeResponse);
	cmdMessenger.sendCmdArg("ArduinoGauge");
	cmdMessenger.sendCmdArg(getSerialNumber());
	cmdMessenger.sendCmdEnd();
}

void onSetTarget() {
	unsigned int motorIndex = cmdMessenger.readInt16Arg();
	unsigned int targetPosition = cmdMessenger.readInt16Arg();

	motors[motorIndex].setPosition(targetPosition);
}

void onZero() {
	unsigned int motorIndex = cmdMessenger.readInt16Arg();

	motors[motorIndex].zero();
}

void onZeroAll() {
	zeroAllMotors();
}

// the setup function runs once when you press reset or power the board
void setup() {
	Serial.begin(57600);

	// Add new line to every command
	cmdMessenger.printLfCr();

	// attach callbacks
	attachCommandCallbacks();

	// Send startup status to PC
	cmdMessenger.sendCmd(kStatus, "ArduinoGauge device has started");

	// Zero all motors
	zeroAllMotors();
}

// the loop function runs over and over again until power down or reset
void loop() {
	// Process incoming serial data and perform callbacks
	cmdMessenger.feedinSerialData();

	// Update motor positions
	for (size_t i = 0; i < NUM_MOTORS; i++) {
		motors[i].update();
	}
}

String getSerialNumber() {
	// TODO: Fix Onboard Serial Number Generator
	return "00000001";
}

void zeroAllMotors() {
	for (size_t i = 0; i < NUM_MOTORS; i++) {
		motors[i].zero();
	}
}


