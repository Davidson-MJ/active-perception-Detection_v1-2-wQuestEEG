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

using System.Collections;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;

namespace MMBTS
{
	public sealed class SerialPort_MMBTS_Test : MonoBehaviour
	{
		public InputField serialPort_InputField;
		public Slider triggersValue_Slider;
		public Slider triggersDuration_Slider;
		public Slider triggersInterval_Slider;
		public Button startStopTriggers_Button;
		public Toggle logTriggers_Toggle;

		private Text _triggersValue_Text;
		private Text _triggersDuration_Text;
		private Text _triggersInterval_Text;

		private Text _startStopTriggers_Button_Text;
		private Image _startStopTriggers_Button_Image;
		private Color _startStopTriggers_Button_OriginalColor;
		
		private Coroutine _triggersLoop = null;
		private SerialPort_MMBTS _serialPort_MMBTS = new SerialPort_MMBTS();

		private void Awake()
		{
			serialPort_InputField.onEndEdit.AddListener(_Connect);
			serialPort_InputField.interactable = true;

			_triggersValue_Text = triggersValue_Slider.GetComponentInChildren<Text>();
			triggersValue_Slider.onValueChanged.AddListener((float val) => _UpdateSliderText(val, "0", _triggersValue_Text));
			triggersValue_Slider.wholeNumbers = true;
			triggersValue_Slider.maxValue = byte.MaxValue;
			triggersValue_Slider.value = triggersValue_Slider.minValue = 1;

			_triggersDuration_Text = triggersDuration_Slider.GetComponentInChildren<Text>();
			triggersDuration_Slider.onValueChanged.AddListener((float val) => _UpdateSliderText(val, "0.00", _triggersDuration_Text));
			triggersDuration_Slider.wholeNumbers = false;
			triggersDuration_Slider.maxValue = 2f;
			triggersDuration_Slider.minValue = 0.01f;
			triggersDuration_Slider.value = 0.05f;

			_triggersInterval_Text = triggersInterval_Slider.GetComponentInChildren<Text>();
			triggersInterval_Slider.onValueChanged.AddListener((float val) => _UpdateSliderText(val, "0.00", _triggersInterval_Text));
			triggersInterval_Slider.wholeNumbers = false;
			triggersInterval_Slider.maxValue = 2f;
			triggersInterval_Slider.minValue = 0.01f;
			triggersInterval_Slider.value = 0.50f;

			_startStopTriggers_Button_Text = startStopTriggers_Button.GetComponentInChildren<Text>();
			_startStopTriggers_Button_Image = startStopTriggers_Button.GetComponent<Image>();
			_startStopTriggers_Button_OriginalColor = _startStopTriggers_Button_Image.color;
			startStopTriggers_Button.onClick.AddListener(_StartStopTriggersLoop);
			startStopTriggers_Button.interactable = false;

			logTriggers_Toggle.isOn = false;
		}

		private void Start()
		{
			string msg = "[SerialPort_MMBTS_Test] Available ports : ";
			string[] portNames = SerialPort.GetPortNames();
			foreach(string port in portNames)
			{
				msg += port + " ; ";
			}
			UnityEngine.Debug.Log(msg);
		}

		private void OnDestroy()
		{
			if (_triggersLoop != null)
			{
				_StopTriggersLoop();
			}
		}

		private void _Connect(string pSerialPort)
		{
			string msg;
			if(_serialPort_MMBTS.Connect(out msg, pSerialPort))
			{
				startStopTriggers_Button.interactable = true;
				Debug.Log(msg);
			}
			else
			{
				startStopTriggers_Button.interactable = false;
				Debug.LogWarning(msg);
			}
		}

		private void _UpdateSliderText(float pVal, string pFormat, Text pText)
		{
			pText.text = pVal.ToString(pFormat);
		}

		private void _StartStopTriggersLoop()
		{
			if (_triggersLoop != null)
			{
				_StopTriggersLoop();
				_startStopTriggers_Button_Text.text = "Start sending triggers";
				_startStopTriggers_Button_Image.color = _startStopTriggers_Button_OriginalColor;
				serialPort_InputField.interactable = true;
			}
			else
			{
				_triggersLoop = StartCoroutine(_SendTriggersLoopCoroutine());
				_startStopTriggers_Button_Text.text = "Stop sending triggers";
				_startStopTriggers_Button_Image.color = Color.red;
				serialPort_InputField.interactable = false;
			}
		}

		private void _StopTriggersLoop()
		{
			StopCoroutine(_triggersLoop);
			_triggersLoop = null;
			_serialPort_MMBTS.SendTrigger(byte.MinValue);
		}

		private IEnumerator _SendTriggersLoopCoroutine()
		{
			while (true)
			{
				bool log = logTriggers_Toggle.isOn;
				byte trigger = (byte)triggersValue_Slider.value;
				
				_serialPort_MMBTS.SendTrigger(trigger);
				if(log)
				{
					Debug.Log("[SerialPort_MMBTS_Test] Trigger sent : " + trigger);
				}

				yield return new WaitForSecondsRealtime(triggersDuration_Slider.value);
				
				_serialPort_MMBTS.SendTrigger(byte.MinValue);
				if (log)
				{
					Debug.Log("[SerialPort_MMBTS_Test] Trigger reset (0)");
				}

				yield return new WaitForSecondsRealtime(triggersInterval_Slider.value);
			}
		}
	}
}