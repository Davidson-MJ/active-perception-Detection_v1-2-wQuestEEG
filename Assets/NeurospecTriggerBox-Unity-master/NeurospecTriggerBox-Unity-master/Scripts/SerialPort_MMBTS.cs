/******************************************************************************************************************************************************
* MIT License																																		  *
*																																					  *
* Copyright (c) 2021																																  *
* Emmanuel Badier <emmanuel.badier@gmail.com>																										  *
* 																																					  *
* Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),  *
* to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,  *
* and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:          *
* 																																					  *
* The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.					  *
* 																																					  *
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, *
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 																							  *
* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 		  *
* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.							  *
******************************************************************************************************************************************************/

using System;
using System.IO.Ports;
using System.Threading;

namespace MMBTS
{
	/// <summary>
	/// To use the MMBT-S Box from NeuroSpec: https://shop.neurospec.com/mmbt-s-trigger-interface-box
	/// </summary>
	public sealed class SerialPort_MMBTS
	{
		private SerialPort _serialPort = new SerialPort();

		/// <summary>
		/// Ensures serial port connection is always released.
		/// </summary>
		~SerialPort_MMBTS()
		{
			Disconnect();
		}

		/// <summary>
		/// Try to connect to the given COM port.
		/// Automatically disconnect from any previous connection : you can call this method many times.
		/// Returns true if successfully connected, false otherwise.
		/// </summary>
		public bool Connect(out string pMsg, string pPortName = "COM4")
		{
			bool success = false;

			// Always Disconnect from any previous connection.
			Disconnect();
			// Wait until previous connection is closed.
			while (_serialPort.IsOpen)
			{
				Thread.Sleep(100);
			}

			// Try a new connection.
			try
			{
				// Configure.
				_serialPort.PortName = pPortName;
				_serialPort.BaudRate = 9600;
				_serialPort.Parity = Parity.None;
				_serialPort.DataBits = 8;
				_serialPort.StopBits = StopBits.One;
				_serialPort.Handshake = Handshake.None;
				_serialPort.DtrEnable = true;
				_serialPort.RtsEnable = false;
				//_serialPort.Encoding = Encoding.UTF8; // No Encoding needed here.
				// Any timeout value above is really not accurate in the case of Trigger
				// The timeout is set only to not block application in case of a serial port issue.
				_serialPort.ReadTimeout = 500;
				_serialPort.WriteTimeout = 500;

				// Open a new Connection.
				_serialPort.Open();
				pMsg = "[SerialPort_MMBTS] successfully connected to port : " + pPortName + ".";
				success = true;
			}
			catch (Exception e)
			{
				pMsg = "[SerialPort_MMBTS] failed to connect to port " + pPortName + " : " + e.Message;
			}

			return success;
		}

		/// <summary>
		/// Disconnect from the COM port.
		/// </summary>
		public void Disconnect()
		{
			if (_serialPort.IsOpen)
			{
				_serialPort.Close();
			}
		}

		/// <summary>
		/// Send a trigger to the COM port.
		/// </summary>
		public void SendTrigger(byte pData)
		{
			if (_serialPort.IsOpen)
			{
				_serialPort.Write(new byte[1] { pData }, 0, 1);
			}
		}

		/// <summary>
		/// Read a trigger from the COM port.
		/// Not tested.
		/// </summary>
		public byte ReadTrigger()
		{
			if (_serialPort.IsOpen)
			{
				try
				{
					int data = _serialPort.ReadByte();
					// the result is a byte or -1 (error : no data to read).
					return data != -1 ? (byte)data : byte.MinValue; // -1 translated to 0
				}
				catch (System.TimeoutException)
				{
					// Always return 0 in case of Timeout.
					return byte.MinValue;
				}
			}
			// Always return 0 if serial port closed.
			return byte.MinValue;
		}
	}
}