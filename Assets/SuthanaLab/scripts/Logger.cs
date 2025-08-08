using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

static class Logger
{
    static FileStream fstream;
    static StreamWriter swriter;
    public static bool fileopen;
    static System.DateTime timeZero;
    static System.DateTime clocker;
    static string timestamp;
    static string logmessage;
    
    public static Vector3 gazeDir;
    public static string gazeTarget="none";
    public static float leftPupilSize;
    public static float rightPupilSize;
    
    static string writePath;

    public static void init(string filenameCore) {
        if (!fileopen)
        {
            fileopen = true;
            timeZero = System.DateTime.Now;
            clocker = timeZero;
            string logname = filenameCore + "_" + timeZero.ToString("MM_dd_yyyy_HH_mm");            
            
        #if UNITY_EDITOR
            writePath = Path.Combine(Application.streamingAssetsPath, logname + ".csv");
        #else
            writePath = Path.Combine(Application.persistentDataPath, logname + ".csv");
        #endif
            Debug.Log("<color=cyan>filepath: " + writePath + "</color>");

            fstream = new FileStream(writePath, FileMode.Create, FileAccess.ReadWrite);
            swriter = new StreamWriter(fstream);
			//printing headers
            swriter.Write(	"Timestamp	,"+
							"Apptime	,"+
                            "State      ,"+
							"Headposx	,"+
							"Headposy	,"+
							"Headposz	,"+
							"Headrotx	,"+
							"Headroty	,"+
							"Headrotz	,"+
							"EyeDirx	," +
                            "EyeDiry	," +
                            "EyeDirz	," +
                            "LeftPupilDiameter,"+
                            "RightPupilDiameter,"+
                            "GazeTarget	," +
                            "optiposx	," +
                            "optiposy	," +
                            "optiposz	," +
                            "optirotx	," +
                            "optiroty	," +
                            "optirotz	," +
                            "Statusmsg\n");
        }
    }
    public static void reinit(string Filename) {
        if (!fileopen)
        {
            fstream = new FileStream(Filename, FileMode.Append, FileAccess.Write);
            swriter = new StreamWriter(fstream);
            fileopen = true;
        }
    }
    public static string terminate() {
        string fullFilename="";
        if (fileopen)
        {            
            fileopen = false;
            swriter.Close();
            fstream.Close();
            Debug.Log("<color=cyan>Logfile saved and closed!</color>");
            fullFilename = writePath;
        }
        return fullFilename;
    }
    public static void Loginfo( float apptime		=-1f,
                                float posx			=-1f,
                                float posy			=-1f,
                                float posz			=-1f, 
                                float rotx			=-1f,
                                float roty			=-1f, 
                                float rotz			=-1f,
                                string state        ="",
                                float optiposx      = -1f,
                                float optiposy      = -1f,
                                float optiposz      = -1f,
                                float optirotx      = -1f,
                                float optiroty      = -1f,
                                float optirotz      = -1f,
                                string message		="#") {
        //apptime,posx,posy,posz,rotx,roty,rotz,rotw,message
        logmessage = apptime 		+ ","
                    + state         + ","
					+ posx 			+ "," 
					+ posy 			+ "," 
					+ posz 			+ "," 
					+ rotx 			+ "," 
					+ roty 			+ "," 
					+ rotz 			+ "," 
                    + gazeDir.x     + ","
                    + gazeDir.y     + ","
                    + gazeDir.z     + ","
                    + leftPupilSize + ","
                    + rightPupilSize+ ","
                    + gazeTarget    + ","
                    + optiposx      + ","
                    + optiposy      + ","
                    + optiposz      + ","
                    + optirotx      + ","
                    + optiroty      + ","
                    + optirotz      + ","
                    + message;

        clocker = System.DateTime.Now;//timeZero.AddSeconds(Time.time);
        timestamp = clocker.ToString("HH:mm:ss:fff");

        if (message != "#" && !message.StartsWith("["))
            Debug.Log("<color=cyan>" + timestamp+"|"+logmessage + "</color>");
        if (fileopen)
        {            
            swriter.Write(timestamp + "," + logmessage + "\n");            
        }
    }    
}
