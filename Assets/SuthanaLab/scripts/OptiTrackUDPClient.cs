using System;
using System.Net;
using System.Net.Sockets;

using UnityEngine;

using System.Threading;

public class MyStateObject
{
	public Socket workSocket = null;
	public const int BUFFER_SIZE = 65507;
	public byte[] buffer = new byte[BUFFER_SIZE];
}

// OptiTrackUDPClient is a class for connecting to OptiTrack Arena Skeleton data
// and storing in a general Skeleton class object for access by Unity characters
public class OptiTrackUDPClient
{
	//The geniuses removed a bunch of info from the stream in newer versions
	public int natnetVersion = 3;
	public int dataPort = 1511;
	public int commandPort = 1510;
	public string multicastIPAddress = "239.255.42.99";
	public string localIPAddress = "127.0.0.1";
	public string motiveIPAddress = "127.0.0.1";

	public string rigidBody2Stream = "randomBox";

	bool skeletonsPresent;
	public bool bNewData = false;
	public Skeleton skelTarget = null;
	public RigidBody rbTarget = null;
	bool debugMessages=false;

	Socket motiveSocket = null;
	Socket sockCommand = null;
	String strFrame = "";
	public int commcounter = 0;


	public const int BUFFER_SIZE = 65507;
	Thread udpListenerThread;
	bool stopUDPlisten;
	public string received_data;

	public OptiTrackUDPClient()
	{
	}

	public void ConnectThreads() {
		rbTarget = new RigidBody();
		udpListenerThread = new Thread(new ThreadStart(UDPlistenerThread));
		udpListenerThread.Start();
	}
	void UDPlistenerThread()
	{
		//IPEndPoint groupEP = new IPEndPoint(IPAddress.Parse("192.168.1.114"), dataPort);
		IPEndPoint groupEP = new IPEndPoint(IPAddress.Any, dataPort);

		motiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		motiveSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		motiveSocket.Bind(groupEP);

		byte[] receive_byte_array = new byte[BUFFER_SIZE];

		// connect socket to multicast group
		IPAddress ip = IPAddress.Parse(multicastIPAddress);
		motiveSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, IPAddress.Parse(localIPAddress)));


		IPEndPoint ipep;
		MyStateObject so;
		
		// create command socket
		sockCommand = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		ipep = new IPEndPoint(IPAddress.Parse(localIPAddress), 0);
		try
		{
			sockCommand.Bind(ipep);
		}
		catch (Exception ex)
		{
			received_data = "bind exception : " + ex.Message;
			bNewData = true;
		}
		// asynch - begin listening
		so = new MyStateObject();
		so.workSocket = sockCommand;
		sockCommand.BeginReceive(so.buffer, 0, MyStateObject.BUFFER_SIZE, 0, new AsyncCallback(AsyncReceiveCallback), so);

		received_data = "starting to listen";
		//bNewData = true;

		try
		{
			while (!stopUDPlisten)
			{
				//Console.WriteLine("Waiting for broadcast");

				// this is the line of code that receives the broadcase message.
				// It calls the receive function from the object listener (class UdpClient)
				// It passes to listener the end point groupEP.
				// It puts the data from the broadcast message into the byte array
				// named received_byte_array.
				// I don't know why this uses the class UdpClient and IPEndPoint like this.
				// Contrast this with the talker code. It does not pass by reference.
				// Note that this is a synchronous or blocking call.

				//receive_byte_array = listener.Receive(ref groupEP);
				motiveSocket.Receive(receive_byte_array);
				received_data = "Received broadcast from ip: " + groupEP.Address + " port: " + groupEP.Port + " family:" + groupEP.AddressFamily;
				ReadPacket(receive_byte_array);
				//Console.WriteLine("Received a broadcast from {0}", groupEP.ToString());
				//received_data += "|" + System.Text.Encoding.ASCII.GetString(receive_byte_array, 0, receive_byte_array.Length);
				//Console.WriteLine("data follows \n{0}\n\n", received_data);
				commcounter++;
				bNewData = true;
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}
		try { motiveSocket.Close(); }
		catch { }
		
	}

	public bool Connect()
	{
		IPEndPoint ipep;
		MyStateObject so;

		rbTarget = new RigidBody();

		//Debug.Log("[UDPClient] Connecting.");
		
		// create data socket
		motiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		motiveSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

		//ipep = new IPEndPoint(IPAddress.Parse(localIPAddress), dataPort);
		ipep = new IPEndPoint(IPAddress.Any, dataPort);

		try
		{			
			motiveSocket.Bind(ipep);
		}
		catch (Exception ex)
		{
			Debug.Log("bind exception : " + ex.Message);
		}
				
		// connect socket to multicast group
		IPAddress ip = IPAddress.Parse(multicastIPAddress);
		motiveSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(ip, IPAddress.Parse(localIPAddress)));
		

		so = new MyStateObject();
#if true
		// asynch - begin listening
		so.workSocket = motiveSocket;
		motiveSocket.BeginReceive(so.buffer, 0, MyStateObject.BUFFER_SIZE, 0, new AsyncCallback(AsyncReceiveCallback), so);
#else
        // synch - read 1 frame
        int nBytesRead = s.Receive(so.buffer);
        strFrame = String.Format("Received Bytes : {0}\n", nBytesRead);
        if(nBytesRead > 0)
            ReadPacket(so.buffer);
        textBox1.Text = strFrame;
#endif


		// create command socket
		sockCommand = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		ipep = new IPEndPoint(IPAddress.Parse(localIPAddress), 0);
		try
		{
			sockCommand.Bind(ipep);
		}
		catch (Exception ex)
		{
			Debug.Log("bind exception : " + ex.Message);
		}
		// asynch - begin listening
		so = new MyStateObject();
		so.workSocket = sockCommand;
		sockCommand.BeginReceive(so.buffer, 0, MyStateObject.BUFFER_SIZE, 0, new AsyncCallback(AsyncReceiveCallback), so);

		return true;
	}

	public void RequestDataDescriptions()
	{
		RequestData((ushort)4);
		//Debug.Log("[UDPClient] from RequestDescription");
	}
	public void RequestFrameOfData()
	{
		RequestData((ushort)6);
		//Debug.Log("[UDPClient] from RequestFrameOfData");
	}
	bool RequestData(ushort RefCode)
	{
		if (sockCommand != null)
		{
			Byte[] message = new Byte[100];
			int offset = 0;
			ushort[] val = new ushort[1];
			val[0] = RefCode;
			Buffer.BlockCopy(val, 0, message, offset, 1 * sizeof(ushort));
			offset += sizeof(ushort);
			val[0] = 0;
			Buffer.BlockCopy(val, 0, message, offset, 1 * sizeof(ushort));
			offset += sizeof(ushort);

			IPEndPoint ipep = new IPEndPoint(IPAddress.Parse(motiveIPAddress), commandPort);
			int iBytesSent = sockCommand.SendTo(message, ipep);
			//Debug.Log("[UDPClient] (Bytes sent:" + iBytesSent + ")");
		}

		return true;
	}


	// Async socket reader callback - called by .net when socket async receive procedure receives a messag 
	private void AsyncReceiveCallback(IAsyncResult ar)
	{
		MyStateObject so = (MyStateObject)ar.AsyncState;
		Socket s = so.workSocket;
		int read = s.EndReceive(ar);
		//Debug.Log("[UDPClient] Received Packet (" + read + " bytes)");	
		if (read > 0)
		{
			// unpack the data
			ReadPacket(so.buffer);
			if (s == motiveSocket)
				bNewData = true;    // indicate to update character

			// listen for next frame
			s.BeginReceive(so.buffer, 0, MyStateObject.BUFFER_SIZE, 0, new AsyncCallback(AsyncReceiveCallback), so);
		}

	}

	private void ReadPacket(Byte[] b)
	{
		int offset = 0;
		int nBytes = 0;
		int[] iData = new int[100];
		float[] fData = new float[500];
		char[] cData = new char[500];

		Buffer.BlockCopy(b, offset, iData, 0, 2); offset += 2;
		int messageID = iData[0];

		Buffer.BlockCopy(b, offset, iData, 0, 2); offset += 2;
		nBytes = iData[0];

		if (debugMessages)
			Debug.Log("[UDPClient] Processing Received Packet (Message ID : " + messageID + ")");

		received_data = "[UDPClient] Processing Received Packet(Message ID: " + messageID + ")";
		if (messageID == 5)     // Data descriptions
		{
			strFrame = ("[UDPClient] Read DataDescriptions");
			received_data = strFrame;

			Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
			strFrame += String.Format(" Dataset Count: {0}\n", iData[0]);
			received_data = strFrame;
			int nDatasets = iData[0];

			for (int i = 0; i < nDatasets; i++)
			{
				//print("Dataset %d\n", i);

				Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
				strFrame += String.Format("Dataset # {0} (type: {1})\n", i, iData[0]);
				received_data = strFrame;
				int type = iData[0];

				if (type == 0)   // markerset
				{
					// name
					string strName = "";
					while (b[offset] != '\0')
					{
						Buffer.BlockCopy(b, offset, cData, 0, 1); offset += 1;
						strName += cData[0];
					}
					offset += 1;
					strFrame += String.Format("MARKERSET (Name: {0})\n", strName);
					received_data = strFrame;

					if (strName == rigidBody2Stream) {						
						rbTarget.name = strName;
						if (debugMessages)
							Debug.Log("[UDPClient] rigidbody renamed successfully!");
						received_data += "[UDPClient] rigidbody renamed successfully!";
					}
					//bNewData = true;
					// marker data
					Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
					strFrame += String.Format("marker count: {0}\n", iData[0]);
					
					int nMarkers = iData[0];

					for (int j = 0; j < nMarkers; j++)
					{
						strName = "";
						while (b[offset] != '\0')
						{
							Buffer.BlockCopy(b, offset, cData, 0, 1); offset += 1;
							strName += cData[0];
						}
						offset += 1;
						strFrame += String.Format("Name : {0}\n", strName);
					}
				}
				else if (type == 1)   // rigid body
				{
					/*
	                if(major >= 2)
	                {
	                    // name
	                    char szName[MAX_NAMELENGTH];
	                    strcpy(szName, ptr);
	                    ptr += strlen(ptr) + 1;
	                    printf("Name: %s\n", szName);
	                }
	
	                int ID = 0; memcpy(&ID, ptr, 4); ptr +=4;
	                printf("ID : %d\n", ID);
	             
	                int parentID = 0; memcpy(&parentID, ptr, 4); ptr +=4;
	                printf("Parent ID : %d\n", parentID);
	                
	                float xoffset = 0; memcpy(&xoffset, ptr, 4); ptr +=4;
	                printf("X Offset : %3.2f\n", xoffset);
	
	                float yoffset = 0; memcpy(&yoffset, ptr, 4); ptr +=4;
	                printf("Y Offset : %3.2f\n", yoffset);
	
	                float zoffset = 0; memcpy(&zoffset, ptr, 4); ptr +=4;
	                printf("Z Offset : %3.2f\n", zoffset);
					*/
				}
				else if (type == 2)   // skeleton
				{
					InitializeSkeleton(b, offset);

				}

			}
			// next dataset
			if (debugMessages)
				Debug.Log(strFrame);
		}

		//else if (messageID == 7)    // Frame of Mocap Data
		else if (messageID == 7)    // new messageID for frame of Mocap Data
		{
			if (debugMessages)
				Debug.Log("[UDPClient] Reading FrameOfMocapData");

			strFrame = "[UDPClient] Read FrameOfMocapData\n";
			received_data = strFrame;
			//bNewData = true;
			Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
			strFrame += String.Format("Frame # : {0}\n", iData[0]);
			received_data = strFrame;
			//bNewData = true;

			//Debug.Log("[UDPClient] Reading markersets");
			// MarkerSets
			Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
			int nMarkerSets = iData[0];
			strFrame += String.Format("MarkerSets # : {0}\n", iData[0]);
			received_data = strFrame;
			for (int i = 0; i < nMarkerSets; i++)
			{
				String strName = "";
				int nChars = 0;
				while (b[offset + nChars] != '\0')
				{
					nChars++;
				}
				strName = System.Text.Encoding.ASCII.GetString(b, offset, nChars);
				offset += nChars + 1;


				Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
				strFrame += String.Format("Marker Count : {0}\n", iData[0]);
				received_data = strFrame;

				nBytes = iData[0] * 3 * 4;
				Buffer.BlockCopy(b, offset, fData, 0, nBytes); offset += nBytes;
			}
			//Debug.Log(strFrame);

			//Debug.Log("[UDPClient] Reading other markers");
			// Other Markers
			Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
			int nOtherMarkers = iData[0];
			strFrame += String.Format("Other Markers : {0}\n", iData[0]);
			received_data = strFrame;
			nBytes = iData[0] * 3 * 4;
			Buffer.BlockCopy(b, offset, fData, 0, nBytes); offset += nBytes;
			//Debug.Log(strFrame);

			if (debugMessages)
				Debug.Log("[UDPClient] Reading rigidbodies");

			// Rigid Bodies
			RigidBody rb = new RigidBody();
			Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
			int nRigidBodies = iData[0];
			strFrame += String.Format("Rigid Bodies : {0}\n", iData[0]);
			received_data = strFrame;
			//bNewData = true;

			if (debugMessages)
				Debug.Log("[UDPClient] number of rigidbodies: "+nRigidBodies);
			received_data = "[UDPClient] number of rigidbodies: " + nRigidBodies;
			//bNewData = true;

			for (int i = 0; i < nRigidBodies; i++)
			{
				ReadRB(b, ref offset, rb);
			}

			received_data += "[UDPClient] read rigid body data";
			//bNewData = true;

			if (debugMessages)
				Debug.Log("right before setting values to rb target");

			if (debugMessages)
				Debug.Log("rigid body received, pos:" + rb.pos + " orientation:" + rb.ori);
			
			rbTarget.pos = rb.pos; rbTarget.ori = rb.ori;
			received_data += "rigid body received, pos:" + rb.pos + " orientation:" + rb.ori;
			//bNewData = true;

			if (debugMessages)
			{
				//Debug.Log(strFrame);
				Debug.Log("[UDPClient] Reading skeletons");
			}

			// Skeletons
			Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
			int nSkeletons = iData[0];

			if (debugMessages)
				Debug.Log("[UDPClient] Found skeletons: "+nSkeletons);

			received_data += "[UDPClient] Found skeletons: " + nSkeletons;
			//bNewData = true;


			if (!skeletonsPresent || nSkeletons == 0)
				return;

			
			strFrame += String.Format("Skeletons : {0}\n", iData[0]);
			for (int i = 0; i < nSkeletons; i++)
			{
				// ID
				Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
				skelTarget.ID = iData[0];
				// # rbs (bones) in skeleton
				Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
				skelTarget.nBones = iData[0];
				for (int j = 0; j < skelTarget.nBones; j++)
				{
					ReadRB(b, ref offset, skelTarget.bones[j]);
				}
			}
			//Debug.Log("[UDPClient] number of bones: "+skelTarget.nBones);

			if (debugMessages)
				Debug.Log(strFrame);

			// frame latency
			Buffer.BlockCopy(b, offset, fData, 0, 4); offset += 4;

			// end of data (EOD) tag
			Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;

			if (debugMessages)
				Debug.Log(strFrame);

			// debug
			String str = String.Format("Skel ID : {0}", skelTarget.ID);
			for (int i = 0; i < skelTarget.nBones; i++)
			{
				String st = String.Format(" Bone {0}: ID: {1}    raw pos ({2:F2},{3:F2},{4:F2})  raw ori ({5:F2},{6:F2},{7:F2},{8:F2})",
					i, skelTarget.bones[i].ID,
					skelTarget.bones[i].pos[0], skelTarget.bones[i].pos[1], skelTarget.bones[i].pos[2],
					skelTarget.bones[i].ori[0], skelTarget.bones[i].ori[1], skelTarget.bones[i].ori[2], skelTarget.bones[i].ori[3]);
				str += "\n" + st;
			}
			//Debug.Log(str);

			if (skelTarget.bNeedBoneLengths)
				skelTarget.UpdateBoneLengths();

			if (debugMessages)
				Debug.Log("[UDPClient] Finished reading skeletons info");
		}
		else// if (messageID == 100)
		{
			if (debugMessages)
				Debug.Log("[UDPClient] Packet Read: Unrecognized Request : "+messageID);
		}

	}

	// Unpack RigidBody data
	private void ReadRB(Byte[] b, ref int offset, RigidBody rb)
	{
		int[] iData = new int[100];
		float[] fData = new float[100];

		// RB ID
		Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
		int iSkelID = iData[0] >> 16;           // hi 16 bits = ID of bone's parent skeleton
		int iBoneID = iData[0] & 0xffff;        // lo 16 bits = ID of bone
												//rb.ID = iData[0]; // already have it from data descriptions

		received_data += "[UDPClient] trying to read rigid body data...";
		//bNewData = true;

		// RB pos
		float[] pos = new float[3];
		Buffer.BlockCopy(b, offset, pos, 0, 4 * 3); offset += 4 * 3;
		received_data += "[UDPClient] read rigid body data pos: "+pos;
		//bNewData = true;
		rb.pos.x = -pos[0]; rb.pos[1] = pos[1]; rb.pos[2] = pos[2];
		received_data += "[UDPClient] read rigid body data rb.pos: " + rb.pos;
		//bNewData = true;


		// RB ori
		float[] ori = new float[4];
		Buffer.BlockCopy(b, offset, ori, 0, 4 * 4); offset += 4 * 4;
		received_data += "[UDPClient] read rigid body data ori: "+ori;
		//bNewData = true;
		rb.ori.x = -ori[0]; rb.ori.y = ori[1]; rb.ori.z = ori[2]; rb.ori.w = -ori[3];
		received_data += "[UDPClient] read rigid body data rb.ori: " + rb.ori;
		//bNewData = true;

		if (natnetVersion >= 3)
		{
			received_data += "[UDPClient] reading rigid body data returning as the following data was removed";
			//bNewData = true;
			return;
		}

		// RB's markers
		Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
		int nMarkers = iData[0];
		received_data = "[UDPClient] reading rigid body data, nb.markers #: " + nMarkers;
		//bNewData = true;
		Buffer.BlockCopy(b, offset, fData, 0, 4 * 3 * nMarkers); offset += 4 * 3 * nMarkers;

		// RB's marker ids
		Buffer.BlockCopy(b, offset, iData, 0, 4 * nMarkers); offset += 4 * nMarkers;

		// RB's marker sizes
		Buffer.BlockCopy(b, offset, fData, 0, 4 * nMarkers); offset += 4 * nMarkers;

		// RB mean error
		Buffer.BlockCopy(b, offset, fData, 0, 4); offset += 4;
		received_data = "[UDPClient] finished full read of rigid body data";
		//bNewData = true;
	}

	void InitializeSkeleton(Byte[] b, int offset)
	{
		skeletonsPresent = true;
		int[] iData = new int[100];
		float[] fData = new float[500];
		char[] cData = new char[500];

		Debug.Log("[UDPClient] Entered initialize skeleton...");

		string strName = "";
		while (b[offset] != '\0')
		{
			Buffer.BlockCopy(b, offset, cData, 0, 1); offset += 1;
			strName += cData[0];
		}
		offset += 1;
		strFrame += String.Format("SKELETON (Name: {0})\n", strName);
		skelTarget.name = strName;

		Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
		strFrame += String.Format("SkeletonID: {0}\n", iData[0]);
		skelTarget.ID = iData[0];

		Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
		strFrame += String.Format("nRigidBodies: {0}\n", iData[0]);
		skelTarget.nBones = iData[0];

		Debug.Log("[UDPClient] Trying to initialize skeleton...");

		for (int j = 0; j < skelTarget.nBones; j++)
		{
			// RB name
			string strRBName = "";
			while (b[offset] != '\0')
			{
				Buffer.BlockCopy(b, offset, cData, 0, 1); offset += 1;
				strRBName += cData[0];
			}
			offset += 1;
			strFrame += String.Format("RBName: {0}\n", strRBName);
			skelTarget.bones[j].name = strRBName;

			// RB ID
			Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
			int iSkelID = iData[0] >> 16;           // hi 16 bits = ID of bone's parent skeleton
			int iBoneID = iData[0] & 0xffff;        // lo 16 bits = ID of bone
													//Debug.Log("RBID:" + iBoneID + "  SKELID:"+iSkelID);
			strFrame += String.Format("RBID: {0}\n", iBoneID);
			skelTarget.bones[j].ID = iBoneID;

			// RB Parent
			Buffer.BlockCopy(b, offset, iData, 0, 4); offset += 4;
			strFrame += String.Format("RB Parent ID: {0}\n", iData[0]);
			skelTarget.bones[j].parentID = iData[0];

			// RB local position offset
			Vector3 localPos;
			Buffer.BlockCopy(b, offset, fData, 0, 4); offset += 4;
			strFrame += String.Format("X Offset: {0}\n", fData[0]);
			localPos.x = fData[0];

			Buffer.BlockCopy(b, offset, fData, 0, 4); offset += 4;
			strFrame += String.Format("Y Offset: {0}\n", fData[0]);
			localPos.y = fData[0];

			Buffer.BlockCopy(b, offset, fData, 0, 4); offset += 4;
			strFrame += String.Format("Z Offset: {0}\n", fData[0]);
			localPos.z = fData[0];
			skelTarget.bones[j].pos = localPos;

			Debug.Log("[UDPClient] Added Bone: " + skelTarget.bones[j].name);

		}

		skelTarget.bHasHierarchyDescription = true;

	}

	public void CloseComm() {
		try { motiveSocket.Close(); }
		catch { }
		udpListenerThread.Abort();
	}
}

