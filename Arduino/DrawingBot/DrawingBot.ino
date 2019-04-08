//------------------------------------------------------------------------------------------------------------
// DRAWING BOT
//
// AUTHORS: Tamkin Rahman and Patrick Sarmiento
//
// DESCRIPTION: Implements the Arduino sketch for a 2D plotter. 
//
//  The following hardware is required:
//    - Arduino Uno.
//    - Adafruit Motor Shield v1 (discontinued)
//    - Servo Motor (used for engaging and disengaging the pen).
//    - 2x Stepper Motors (taken from DVD Drives).
//
//  The following software is required: 
//   - Adafruit Motor Shield library: https://github.com/adafruit/Adafruit-Motor-Shield-library
//------------------------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------------------------
//  INCLUDES
//------------------------------------------------------------------------------------------------------------
#include <AFMotor.h>
#include <Servo.h>

//------------------------------------------------------------------------------------------------------------
//  DEFINITIONS AND MACROS
//------------------------------------------------------------------------------------------------------------
// REFERENCE: https://learn.adafruit.com/adafruit-motor-shield/using-stepper-motors
#define MOVEMENT_MODE INTERLEAVE // Gives greatest resolution by alternating between 1 and 2 coils.

// These values were found from tests of the salvaged stepper motors.
#define MOTOR_1_MAX_STEPS 310
#define MOTOR_2_MAX_STEPS 480

#define MAX_X             MOTOR_1_MAX_STEPS
#define MAX_Y             MOTOR_2_MAX_STEPS

#define SERVO_1           10
#define SERVO_2           9
#define PEN_SERVO_PIN     SERVO_1

#define SERVO_LOW         25
#define SERVO_HIGH        33
#define SERVO_DELAY_ms    30

#define MAX_TOKENS        9 // 1 token for the command, and 8 tokens for up to 4 positions.
#define MAX_TOKENS_SIZE   4 // Up to a 3 digit number.
#define MAX_BUFFER_SIZE   ((MAX_TOKENS * MAX_TOKENS_SIZE) + MAX_TOKENS)

#define ACK_CHARACTER     '+'

// If defined, don't activate any motors.
// #define SOFTWARE_TEST

// If defined, print extra debug output.
// #define VERBOSE_OUTPUT

// If defined, print an acknowledgement character after each command.
#define ENABLE_ACK

//------------------------------------------------------------------------------------------------------------
//  GLOBALS
//------------------------------------------------------------------------------------------------------------
// Connect a stepper motor with 48 steps per revolution (7.5 degree)
// to motor port #2 (M3 and M4)
AF_Stepper motor1(48, 1);
AF_Stepper motor2(48, 2);
Servo penServo;

int currentX = 0;
int currentY = 0;
bool penEngaged = false;

//------------------------------------------------------------------------------------------------------------
//  PROTOTYPES
//------------------------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------------
// Splits a given input string based on a given delimiter.
//
// Returns:
//  The number of strings in the output array.
//------------------------------------------------------------------------------------------------
int string_split(
  const char * input,                      // The input string to split.
  const char * delim,                      // The delimiter to split on (e.g. ",")
  char output[MAX_TOKENS][MAX_TOKENS_SIZE] // The array of strings containing the output.
);

//------------------------------------------------------------------------------------------------------------
//  FUNCTIONS
//------------------------------------------------------------------------------------------------------------
int string_split(const char* input, const char * delim, char output[MAX_TOKENS][MAX_TOKENS_SIZE])
{
  char line[80];
  char * token;
  
  int ix = 0;
  
  strcpy(line, input);
  token = strtok(line, delim);
  
  while ((token != NULL) && (ix < MAX_TOKENS))
  {
    strcpy(output[ix], token);
    token = strtok(NULL, delim);

    ix++;
  }

  return ix;
}

//------------------------------------------------------------------------------------------------------------
void engagePen()
{
  if (!penEngaged)
  {
    for (int ix = SERVO_HIGH; ix > SERVO_LOW; ix--)
    {
         penServo.write(ix); 
         delay(SERVO_DELAY_ms);
    }
    penEngaged = true; 
  }
}

//------------------------------------------------------------------------------------------------------------
void disengagePen()
{
  if (penEngaged)
  {
    for (int ix = SERVO_LOW; ix < SERVO_HIGH; ix++)
    {
         penServo.write(ix); 
         delay(SERVO_DELAY_ms);
    }
    penEngaged = false;
  }
}

//------------------------------------------------------------------------------------------------------------
void resetMotors()
{
  motor1.step(MOTOR_1_MAX_STEPS + 20, BACKWARD, MOVEMENT_MODE);
  motor2.step(MOTOR_2_MAX_STEPS + 20, BACKWARD, MOVEMENT_MODE);

  currentX = 0;
  currentY = 0;
}

//------------------------------------------------------------------------------------------------------------
void moveStraightTo(int targetX, int targetY)
{
  // Use Bresenham's Line Algorithm for plotting the line, using integer arithmetic
  // as explained here: https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm#Algorithm_for_integer_arithmetic
  // and here: http://floppsie.comp.glam.ac.uk/Southwales/gaius/gametools/6.html

  int dx = abs(targetX - currentX);
  int dy = abs(targetY - currentY);
  int err = dx - dy;

  int incX = 1;
  int incY = 1;
  int dirX = FORWARD;
  int dirY = FORWARD;
  
  if (currentX >= targetX)
  {
    incX = -1;
    dirX = BACKWARD;
  }
  if (currentY >= targetY)
  {
    incY = -1;
    dirY = BACKWARD;
  }

  int e2 = 0;
  while ((currentX != targetX) || (currentY != targetY))
  {
    #ifdef VERBOSE_OUTPUT
      Serial.print("[");
      Serial.print(currentX);
      Serial.print(", ");
      Serial.print(currentY);
      Serial.print("]\n");
    #endif

    e2 = 2*err;
    if (e2 > -dy)
    {
      err = err - dy;
      // Take a step in the X direction.
      currentX += incX;
      #ifndef SOFTWARE_TEST
        motor1.step(1, dirX, MOVEMENT_MODE);
      #endif
    }
    if (e2 < dx)
    {
      err = err + dx;
      // Take a step in the Y direction.
      currentY += incY;
      #ifndef SOFTWARE_TEST
        motor2.step(1, dirY, MOVEMENT_MODE);
      #endif
    }
  }
}

//------------------------------------------------------------------------------------------------------------
void setup() 
{
  Serial.begin(9600);
  Serial.println("DRAWING BOT STARTED");

  #ifndef SOFTWARE_TEST
    motor1.setSpeed(10);  // 10 rpm   
    motor2.setSpeed(10);
    penServo.attach(PEN_SERVO_PIN);
  #endif

  penEngaged = false;
  penServo.write(SERVO_HIGH);
}

void loop() 
{
  static char tokens[MAX_TOKENS][MAX_TOKENS_SIZE];
  static char char_buffer[MAX_BUFFER_SIZE];
  static int buffer_ix = 0;
  // A simple modified subset of HP-GL is used for the plotter 
  // commands: https://en.wikipedia.org/wiki/HP-GL
  // 
  // The following commands supported:
  // PR - Full reset (reset motors to [0, 0], and disengage the pen)
  // PU - Pen Up
  // PD - Pen Down
  // PA,x1,y1,x2,y2... - Move pen to point (up to 4 pairs of points).
  if (Serial.available() > 0)
  {
    char incoming = Serial.read();
    if (incoming != ';')
    {
      if (incoming != '\n')
      {
        char_buffer[buffer_ix++] = incoming;
      }
    }
    else
    {
      char_buffer[buffer_ix] = '\0';
      buffer_ix = 0;
      #ifdef VERBOSE_OUTPUT
        Serial.print("Received string: ");
        Serial.print(char_buffer);
        Serial.print("\n");
      #endif
      
      int len = string_split(char_buffer, ",", tokens);
      bool command_executed = false;
      if ((len >= 1) && (tokens[0][0] == 'P'))
      {
        if (tokens[0][1] == 'R')
        {
          Serial.print("Received reset command.\n");
          #ifndef SOFTWARE_TEST
            disengagePen();
            resetMotors();
          #endif
          command_executed = true;
        }
        else if (tokens[0][1] == 'U')
        {
          Serial.print("Received pen UP command.\n");
          #ifndef SOFTWARE_TEST
            disengagePen();
          #endif
          command_executed = true;
        }
        else if (tokens[0][1] == 'D')
        {
          Serial.print("Received pen DOWN command.\n");
          #ifndef SOFTWARE_TEST
            engagePen();
          #endif
          command_executed = true;
        }
        else if ((tokens[0][1] == 'A') && (len > 2))
        {
          Serial.print("Plotting ");
          for (int ix = 1; ix < len; ix+=2)
          {
            int x = atoi(tokens[ix]);
            int y = atoi(tokens[ix+1]);
            Serial.print("[");
            Serial.print(x);
            Serial.print(",");
            Serial.print(y);
            Serial.print("] \n");
  
            if (    ((x >= 0) && (x <= MAX_X))
                 && ((y >= 0) && (y <= MAX_Y))
                )
            {
              moveStraightTo(x, y);
            }
          }
          command_executed = true;
        }
        #ifdef ENABLE_ACK
          if (command_executed)
          {
            Serial.print(ACK_CHARACTER);
          }
        #endif
      }
    }
  }
}
