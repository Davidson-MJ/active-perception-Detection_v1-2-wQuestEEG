using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;
using MMBTS;


// this script will establish and handle communication from Unity to the Serial ports.

public class triggerHandler : MonoBehaviour
{
    // Start is called before the first frame update
    public string pPortName = "COM5";
    private string pMsg = "1";
    private byte pData = 1;
	private SerialPort_MMBTS _serialPort_MMBTS = new SerialPort_MMBTS();
    private SerialPort _serialPort = new SerialPort();
    SerialPort sp;
    runExperiment runExperiment;

    int ii = 0;
    float next_time;

    private void Start()
    {


        runExperiment = GameObject.Find("scriptHolder").GetComponent<runExperiment>();

        if (runExperiment.recordEEG) // set up port connections.
        {
            // Get a list of serial port names.
            string[] ports = SerialPort.GetPortNames();

            print("The following serial ports were found:");

            // Display each port name to the console.
            foreach (string port in ports)
            {
                print(port);
            }

            // open specific port:
            sp = new SerialPort("\\\\.\\" + pPortName, 9600);

            if (!sp.IsOpen)
            {
                print("Opening " + pPortName + ", baud 9600");
                sp.Open();
                sp.ReadTimeout = 100;
                sp.Handshake = Handshake.None;
                if (sp.IsOpen) { print("Open"); }
            }

        }







    }
    // try sending a trigger every 5 seconds.
    void Update()
    {
        if (Time.time > next_time)
        {
            if (!sp.IsOpen)
            {
                sp.Open();
                print("opened sp");
            }
            if (sp.IsOpen)
            {
                print("Writing " + ii);
                sp.Write((ii.ToString()));
            }
            next_time = Time.time + 5;
            if (++ii > 9) ii = 0;
        }
    }    

    //public void send(string pMsg)
    //{
    //   // convert string to bytes.
    //    var bytes = System.Text.Encoding.UTF8.GetBytes(pMsg);
    //    _serialPort_MMBTS.SendTrigger(pData);

//}



