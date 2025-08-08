using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public static class SlaveThreadRPi
{
    static bool initialized;
    static UdpClient broadcastSocket;
    static UdpClient slaveSocket;
    static UdpClient vibrationUDPSocket;
    static string vibration_ip = "192.168.50.17";
    static int vibration_port = 50000;


    static int broadcastPort = 8080;
    static int responsePort = 9090;
    static List<IPEndPoint> slaveEndpoints = new List<IPEndPoint>();

    private static object socketLock = new object();

    public static void init()
    {
        if (!initialized)
        {
            initialized = true;

            broadcastSocket = new UdpClient();
            broadcastSocket.EnableBroadcast = true;
            broadcastSocket.Client.ReceiveTimeout = 2000;

            slaveSocket = new UdpClient(responsePort);
            slaveSocket.Client.ReceiveTimeout = 1000;

            DiscoverSlaves();

            vibrationUDPSocket = new UdpClient();
            vibrationUDPSocket.Connect(vibration_ip, vibration_port);
        }
        else
        {
            Debug.Log("Already initialized. Close existing connection before reinitializing.");
        }
    }

    public static void SendVibration()
    {
        byte[] bytesToSend = System.Text.Encoding.ASCII.GetBytes(new char[]{'v'});
        vibrationUDPSocket.Send(bytesToSend, bytesToSend.Length);
        Debug.Log("Vibration");
    }

    private static void DiscoverSlaves()
    {
        byte[] discoverMessage = Encoding.ASCII.GetBytes("Discover");

        IPEndPoint broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
        broadcastSocket.Send(discoverMessage, discoverMessage.Length, broadcastEndpoint);

        Debug.Log("Broadcast message sent. Waiting for slave responses...");

        DateTime startTime = DateTime.Now;
        while ((DateTime.Now - startTime).TotalSeconds < 2)
        {
            try
            {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] response = broadcastSocket.Receive(ref remoteEndPoint);

                string responseMessage = Encoding.ASCII.GetString(response);
                int slavePort = int.Parse(responseMessage);

                IPEndPoint slaveEndpoint = new IPEndPoint(remoteEndPoint.Address, slavePort);

                if (!slaveEndpoints.Contains(slaveEndpoint))
                {
                    int index = slaveEndpoints.FindIndex(ep => ep.Address.GetAddressBytes()[ep.Address.GetAddressBytes().Length - 1] > slaveEndpoint.Address.GetAddressBytes()[slaveEndpoint.Address.GetAddressBytes().Length - 1]);
                    if (index == -1)
                    {
                        slaveEndpoints.Add(slaveEndpoint);
                    }
                    else
                    {
                        slaveEndpoints.Insert(index, slaveEndpoint);
                    }
                }
            }
            catch (SocketException)
            {
                break;
            }
        }

        for(int iEP = 0; iEP < slaveEndpoints.Count; iEP++)
        {
            Debug.Log($"Slave Device ID: {iEP}, IP: {slaveEndpoints[iEP]}");
        }
    }

    public static bool SendMark(int deviceId = -1)
    {
        if(!initialized)
        {
            init();
        }

        if (initialized)
        {
            if(slaveEndpoints.Count > 0)
            {
                if (deviceId < slaveEndpoints.Count)
                {
                    new Thread(() =>
                    {
                        SendMarkThreadFunction(deviceId);
                    }).Start();
                }
                else
                {
                    Debug.Log("Targeted device ID not found.");
                    return false;
                }
            }
            else
            {
                Debug.Log("No slaves were discovered on the network.");
                return false;
            }
        }
        else
        {
            Debug.Log("UDP client not initialized. Call Init() first.");
            return false;
        }
        return true;
    }


    private static void SendMarkThreadFunction(int deviceId)
    {
        for (int i = 0; i < slaveEndpoints.Count; i++)
        {
            IPEndPoint slaveEndpoint;
            if(deviceId == -1)
            {
                slaveEndpoint = slaveEndpoints[i];
            }
            else
            {
                slaveEndpoint = slaveEndpoints[deviceId];
            }
            while (true)
            {
                byte[] bytesToSend = BitConverter.GetBytes(DateTime.Now.Ticks);

                lock (socketLock)
                {
                    try
                    {
                        slaveSocket.Send(bytesToSend, bytesToSend.Length, slaveEndpoint);
                        byte[] bytesReceived = slaveSocket.Receive(ref slaveEndpoint);
                        string receivedData = Encoding.ASCII.GetString(bytesReceived);

                        if (receivedData == "SyncStop")
                        {
                            Debug.Log($"Sync success with {slaveEndpoint}");
                            break;
                        }
                    }
                    catch (SocketException ex)
                    {
                        Debug.Log("Socket Exception: " + ex.Message);
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.Log("Exception: " + ex.Message);
                        break;
                    }
                }
            }

            if(deviceId != -1)
            {
                break;
            }
        }
    }

    public static void CloseComms()
    {
        if (initialized)
        {
            lock (socketLock)
            {
                broadcastSocket.Close();
                slaveSocket.Close();
                vibrationUDPSocket.Close();
                initialized = false;
                slaveEndpoints.Clear();
            }
        }
        else
        {
            Debug.Log("UDP client is already closed or not initialized.");
        }
    }
}

