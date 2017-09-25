using System;
using System.IO.Ports;
using System.Threading;

public class PortChat
{
    static bool _continue;
    static SerialPort _serialPort;

    public static void Main()
    {
        string message;
        Thread readThread = new Thread(Read);

        _serialPort = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One) { NewLine = "\r", ReadTimeout = 100, WriteTimeout = 100 };
        
        _serialPort.Open();
        _continue = true;
        readThread.Start();

        Console.WriteLine("Type QUIT to exit");

        while (_continue)
        {
            message = Console.ReadLine();

            if (message.Equals("quit",StringComparison.OrdinalIgnoreCase))
            {
                _continue = false;
            }
            else
            {
                _serialPort.WriteLine(message);
            }
        }

        readThread.Join();
        _serialPort.Close();
    }

    public static void Read()
    {
        while (_continue)
        {
            try
            {
                string message = _serialPort.ReadLine();
                Console.WriteLine(message);
            }
            catch (TimeoutException) { }
        }
    }
}