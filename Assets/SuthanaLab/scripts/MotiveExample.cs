using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;

public class MotiveExample : MonoBehaviour
{
	// Network - live data source support
	OptiTrackUDPClient udpClient;
	private int dataPort = 1511;
	private string multicastIPAddress = "239.255.42.99";
	public string localIPAddress = "127.0.0.1";
	public string motiveIPAddress = "127.0.0.1";
	

	bool bNeedPerformerSkeletonInit;
	bool bLiveMode;
	string strDebugText;
	public Text debugtt;
	public Transform object2track;
	bool triedonce;

	int fpscounter;
	string fpsstr;
	float fpsSpan = 0.5f;
	float fpsTimer;
	//public Text auxFPS;
	//public Text commFPSUI;

	string commstr;
	float commSpan = 0.5f;
	float commTimer;

	bool requestedDD;


	void Start() {
		strDebugText = "motive example is waiting, press C to connect";
		
		//skelPerformer = new ArenaSkeleton();
		
		//Debug.Log("trying to connect for the first time");
		//ConnectFun();
	}
    void Update()
    {
		fpscounter++;
		fpsTimer -= Time.deltaTime;
		if (fpsTimer <= 0f) {
			fpsTimer = fpsSpan;
			fpsstr = ((float)fpscounter / fpsSpan).ToString("0.00");
			fpscounter = 0;
		}

		debugtt.text = "FPS:"+fpsstr + "| comm:"+commstr+" | "+ strDebugText;
		//auxFPS.text = "FPS:" + fpsstr;
		//commFPSUI.text = "comm(Hz): "+commstr;

		if (Input.GetKeyDown(KeyCode.D))
		{
			//Request DataDescriptors to start requesting frames
			Debug.Log("Requesting DDs");
			udpClient.RequestDataDescriptions();
		}

		if (Input.GetKeyDown(KeyCode.U)) {
			Debug.Log("Requesting next frame");
			udpClient.RequestFrameOfData();
		}
		//Debug.Log("rotation: "+object2track.rotation);
		//update the object
		if (udpClient.bNewData){
			/*
			object2track.position = skelPerformer.bones[0].trans.position;
			object2track.rotation = skelPerformer.bones[0].trans.rotation;
			udpClient.RequestFrameOfData();
			*/
			object2track.position = udpClient.rbTarget.pos;
			object2track.rotation = udpClient.rbTarget.ori;
			
			strDebugText = udpClient.received_data;
			//Debug.Log(udpClient.received_data);

			if (!requestedDD)
			{
				requestedDD = true;
				udpClient.RequestFrameOfData();
			}
			udpClient.bNewData = false;
		}
		commTimer -= Time.deltaTime;
		if (commTimer <= 0f)
		{
			commTimer = commSpan;
			commstr = ((float)udpClient.commcounter / commSpan).ToString("0.00");
			udpClient.commcounter = 0;
		}
	}

    public void ConnectFun()
	{
		#if UNITY_EDITOR
		string hostName = System.Net.Dns.GetHostName();
		var host = System.Net.Dns.GetHostEntry(hostName);
		foreach (var ip in host.AddressList)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork)
			{
				Debug.Log("IP try: " + ip.ToString());
				localIPAddress = ip.ToString();
			}
		}		
		#endif
		triedonce = true;
		strDebugText = "trying to connect...";

		// Connect to a Live Arena session
		udpClient = new OptiTrackUDPClient();

		udpClient.localIPAddress = localIPAddress;
		udpClient.motiveIPAddress = motiveIPAddress;
		udpClient.multicastIPAddress = multicastIPAddress;
		udpClient.dataPort = dataPort;
		//udpClient.rigidBody2Stream = rigidBody2StreamName;
		//udpClient.skelTarget = skelPerformer;   // <-- live data will be stuffed into this skeleton object

		/*
		bool bSuccess = udpClient.Connect();
		
		if (bSuccess)
		{
			udpClient.RequestDataDescriptions();
			int timeout = 0;
			//while ((skelPerformer.nBones == 0) && (timeout < 100))
			while ((udpClient.rbTarget.name == "") && (timeout < 100))
			{
				System.Threading.Thread.Sleep(5);
				timeout++;
			}
			//if (skelPerformer.nBones > 0)
			if (udpClient.rbTarget.name == rigidBody2StreamName)
			{
				bNeedPerformerSkeletonInit = true;
				bLiveMode = true; //untrue, there is an issue reading skeletons, at least when there are no skeletons
				strDebugText += "Live Mode Initialized.";
			}
			else {
				Debug.Log("failed to initialize live mode");
				strDebugText += "failed to go live.";
			}
		}
		*/

		udpClient.ConnectThreads();
		udpClient.RequestDataDescriptions();
		strDebugText += "Live Mode Initialized.";
	}
    //skelPerformer now gets updated in its own thread in the NatNet client script.

    void OnApplicationQuit()
    {
		//close connections
		udpClient.CloseComm();
    }
}