using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.IO;
using System;

public class RPi
{

    bool _socketReady = false;
    private TcpClient _socket;
    private NetworkStream _stream;
    private StreamWriter _writer;
    private string _host = "10.42.0.1";
    private int _port = 8080;

    //Constructor
    public RPi(string host = "10.42.0.1", int port = 8080)
    {
        _host = host;
        _port = port;
        try
        {
            _socket = new TcpClient(_host, _port);
            _stream = _socket.GetStream();
            _writer = new StreamWriter(_stream);
            _socketReady = true;
            Debug.Log("<color=green>Connection established to the RaspberryPi!</color>");
        }
        catch (Exception e)
        {
            _socketReady = false;
            Debug.Log("<color=red>Connection with RaspberryPi failed!!!</color>");
            Debug.Log(e.Message);
        }
    }

    public void sendSsr()
    {
        _writer.Write("r");
        _writer.Flush();
        Debug.Log("ssr");
    }

    public void sendStim()
    {
        _writer.Write("s");
        _writer.Flush();
        Debug.Log("stim");
    }
    public void sendMark()
    {
        if (!_socketReady)
        {
            //try to reconnect
            try
            {
                _socket = new TcpClient(_host, _port);
                _stream = _socket.GetStream();
                _writer = new StreamWriter(_stream);
                _socketReady = true;
                Debug.Log("<color=green>Connection restablished to the RaspberryPi!</color>");
            }
            catch (Exception e)
            {
                _socketReady = false;
                //Abort task
                Logger.terminate();
                Debug.Log("<color=red>Connection with RaspberryPi failed!!!</color>");
                Debug.Log(e.Message);
            }
        }
        else
        {
            _writer.Write("t");
            _writer.Flush();
            Debug.Log("mark");
        }
    }

    public void sendPreMark()
    {
        if (!_socketReady)
        {
            //try to reconnect
            try
            {
                _socket = new TcpClient(_host, _port);
                _stream = _socket.GetStream();
                _writer = new StreamWriter(_stream);
                _socketReady = true;
                Debug.Log("<color=green>Connection restablished to the RaspberryPi!</color>");
            }
            catch (Exception e)
            {
                _socketReady = false;
                //Abort task
                Logger.terminate();
                Debug.Log("<color=red>Connection with RaspberryPi failed!!!</color>");
                Debug.Log(e.Message);
            }
        }
        else
        {
            _writer.Write("p");
            _writer.Flush();
            Debug.Log("premark sent");
        }
    }

    public void sendTest()
    {
        _writer.Write("q");
        _writer.Flush();
        Debug.Log("Test");
    }

    public void sendMagnet()
    {
        _writer.Write("m");
        _writer.Flush();
        Debug.Log("mark");
    }

    public void wandOn()
    {
        _writer.Write("n");
        _writer.Flush();
        Debug.Log("wandON");
    }

    public void wandOff()
    {
        _writer.Write("f");
        _writer.Flush();
        Debug.Log("wandOFF");
    }

    public void sendVibration()
    {
        _writer.Write("v");
        _writer.Flush();
        Debug.Log("Vibration");
    }

    public void closeClient()
    {
        _writer.Write("u");
        _writer.Flush();
        _socket.Close();
        Debug.Log("Closing the client");
    }

    public void closeRPi()
    {
        	//_writer.Write("u");
        	//_writer.Flush();
           _socket.Close();
    }
}
