// ************************************************************************************************
// DRAWING BOT
//
// Repository:
//  None
//
// Description:
//  Helper class for working with a Serial connection.
//
// History:
//  2019-04-04 by Tamkin Rahman
//  - Created.
// ************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;

namespace DrawingBotController.Models
{
    // Reference: https://docs.microsoft.com/en-us/dotnet/api/system.io.ports.serialport?view=netframework-4.7.2
    public class SerialMonitor
    {
        private SerialPort port = new SerialPort();
        private bool connected = false;

        #region Properties
        public bool Connected
        {
            get
            {
                return this.connected;
            }
        }
        public string[] SerialPorts
        {
            get
            {
                return SerialPort.GetPortNames();
            }
        }

        public List<int> BaudRates { get; } = new List<int>()
        {
            110,
            300,
            600,
            1200,
            2400,
            4800,
            9600,
            14400,
            19200,
            38400,
            57600,
            115200,
            230400,
            460800,
            921600
        };
        #endregion

        public SerialMonitor() { }

        public void InitPort(string portname, int baudrate)
        {
            this.port.PortName = portname;
            this.port.BaudRate = baudrate;
        }

        public bool Connect()
        {
            if (!this.port.IsOpen)
            {
                this.port.Open();
                this.connected = true;
            }
            else
            {
                this.connected = true;
            }

            return this.connected;
        }

        public void Disconnect()
        {
            if (this.port.IsOpen)
            {
                this.port.Close();
                this.connected = false;
            }
        }

        public void WriteLine(string line)
        {
            this.port.Write(line);
        }
        
        public int ReadAvailable()
        {
            return this.connected ? this.port.BytesToRead : 0;
        }

        public string ReadExisting()
        {
            return this.port.ReadExisting();
        }
    }
}
