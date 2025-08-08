using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using UnityEngine.XR;
using UnityEngine.UI;
using System.Threading;

public class FearTaskManager : MonoBehaviour
{
    [System.Serializable]
    public struct FearNode {
        public Transform TriggerLocation;
        public GameObject fearStim;
        public float expositionTime;
    }

    enum states { 
        walking,
        unusualLight,
    }
    states state;

    public List<Light> sceneLights;
    bool invertFearColor = false;
    public Color ambientLightColor;
    public Color fearLightColor;
    public Color neutralLightColor;
    public Color fearPracticeColor;
    public Color neutralPracticeColor;
    public float tintamount = 0.2f;
    Color transientColor;
    Color transientTarget;
    Color transienttint;
    
    public float colorChangeRate = 0.2f;
    public float colorFXrange = 12f;
    float colorChangeTimer;
    public bool smoothLightTransition;
    bool colorTransition;
    Transform headTransform;
    Vector3 oldpos;
    public float time2move = 3f;
    float timer2move;

    public List<FearNode> fearStims;
    [Tooltip("Ends the experiment")]
    public Transform FinalTrigger;
    int fear_i;
    float exposingTimer;
    float prepareTimer;
    public float prepareTime = 2f;
    public bool leaveLightON;
    public bool debugMode;

    [Tooltip("in Hertz(cycles per second)")]
    public float lograte = 20f;
    float logtimer;
    bool logging;    
    JoyWalker joyWalker;
    string statestr;
    bool practice;
    bool commstarted;

    //RPi raspberryComm;
    bool useRaspberry = false;
    string raspberryIP = "192.168.1.17";
    int raspberryPort = 8080;
    //RPi raspberrySecComm;
    bool useSecRaspberry = false;
    string raspberrySecIP = "192.168.1.176";
    int raspberrySecPort = 8070;
    bool raspyVibrate;

    FileStream cfgstream;
    StreamReader cfgreader;
    public GameObject ratingsgrp;
    public SphereRater rater;
        
    public ParticleSystem fireflies;
    public Image tintfx;
    bool usefireflies;
    Transform gazeDirector;
    Vector3 gazedir;
    Image transitionFX;
    Color fadecolor = Color.black;
    float fadetarget;
    float fadeTimer=-1f;
    float fadeTime = 3.0f;

    [Tooltip("minimum angle tolerance to show the gaze hint arrows")]
    public float gazetol = 30f;

    public Transform optitrackObj;
    Renderer optiObjrender;

    AudioSource maudio;
    public AudioClip negativeStimSound;
    public AudioClip neutralStimSound;
    StimPicker spicker;

    AutoPlacer aplacer;
    GameObject currentFear;
    float fearTimer;
    bool insideCorridors;
    bool waiting4incorridor;
    
    int taskday; 
    /*
        taskday controls task:
        (1)  day 1 - habituation, acquisition and questions
        rest
        (1)  after rest - extinction and questions
        (2)  day 2 - extinction and questions, habituation     
    */

    bool runningExperiment;
    
    float quitTimer;
    float quitDelay = 0.5f;
    public bool lightChangeOnJumpOut;

    public Canvas externalCommCanvas;
    bool externalCommsRequested = false;
    bool buttonLock = false;

    int locationAcquisition = 0;
    int locationExtinction = 0;
    int currentLocation;
    GameObject instrGO;

    string habituationInstructions;
    string finalHabituationInstructions;
    string acquisitionInstructions;
    string extinctionInstructions;
    string instructions4questions;

    public List<GameObject> initialPathEnforcersActive;
    public List<GameObject> initialPathEnforcersInactive;	

    public enum taskphases {
        acquisition,
        extinction
    }
    public taskphases taskphase;
    
    int stimNumAcquisition;
    int negStimsAcquisition;
    string[] desiredStimsAcq;
    int stimNumExtinction;
    string[] desiredStimsExt;
    public Text userui;
    public Transform beginSpot;

    public List<Transform> debugElements;

    float maxHabituationTime = 180f;
    float habitTimer;
    bool habituated;
    bool needsHabituation;
    bool habituationSpecific;
    enum habituates {
        extinction2acquisition,
        acquisition2extinction,
        finalextinction,
        notransition
    }
    habituates habitstate;

    [SerializeField] public Texture2D fear_light; // The color lightmap texture.
    [SerializeField] public Texture2D neutral_light; // The color lightmap texture.
    
    [SerializeField] public Texture2D practiceFear_light; // The color lightmap texture.
    [SerializeField] public Texture2D practiceNeutral_light; // The color lightmap texture.
    Texture2D envTransientColor;
    Texture2D[] ambientOriginal;

    List<Vector3> acqStimpos;
    List<Vector3> extStimpos;
    int stim_i;
    float triggerDist;
    bool corridorclear;


    /*
    [SerializeField] public Texture2D _dir; // The directional lights texture.
    [SerializeField] public Texture2D _light; // The color lightmap texture.
    [SerializeField] public Texture2D _shadow; // The shadow mask texture.
    */

    public void ChangeBakedLightColor(Texture2D LightBakedColor, bool ToNormal=false)
    {
        
            LightmapData[] lightmaparray = LightmapSettings.lightmaps;
            //LightmapData mapdata = new LightmapData();
            for (var i = 0; i < lightmaparray.Length; i++)
            {
                if (ToNormal) { lightmaparray[i].lightmapColor = ambientOriginal[i]; }
                else
                {
                    //mapdata.lightmapDir = _dir;
                    //mapdata.lightmapColor = _light;
                    //mapdata.shadowMask = _shadow;
                    //lightmaparray[i] = mapdata;
                    lightmaparray[i].lightmapColor = LightBakedColor;
                }
            }
            LightmapSettings.lightmaps = lightmaparray;
        
    }

    void Awake()
    {
        headTransform = Camera.main.transform;
    }
    // Start is called before the first frame update
    void Start()
    {
        habitstate = habituates.notransition;
        fear_i = -1;
        joyWalker = headTransform.GetComponentInParent<JoyWalker>();
        joyWalker.enabled = false;
        spicker = GetComponent<StimPicker>();
        aplacer = GetComponent<AutoPlacer>();

        //hide the rating panel
        ratingsgrp.SetActive(false);

        GameObject optiObj = GameObject.Find("optitrackRigidbody");
        if (optiObj)
            optiObjrender = optiObj.GetComponent<Renderer>();

        if (!optitrackObj && optiObj)
        {
            optitrackObj = optiObj.transform;
        }

        gazeDirector = headTransform.Find("gazeDirector").transform;
        if (gazeDirector)
            gazeDirector.gameObject.SetActive(false);
        if (fireflies)
        {
            fireflies.Clear();
            fireflies.Stop();
        }

        transitionFX = Camera.main.transform.Find("FadeFX").GetChild(1).GetComponent<Image>();
        fadecolor = Color.black;

        instrGO = GameObject.Find("Instructions");

        ReadConfigFile();

        bool resting = false;
        if (PlayerPrefs.HasKey("resting"))
            resting = (PlayerPrefs.GetInt("resting") == 1 ? true : false);

        if (taskday == 2 || resting)
        {
            SetInstructions(extinctionInstructions);
            if (spicker)
                currentLocation = spicker.DefineLocation(locationExtinction);
            else
                currentLocation = aplacer.DefineLocation(locationExtinction);
        }
        else{
            if (spicker)
                currentLocation = spicker.DefineLocation(locationAcquisition);
            else
                currentLocation = aplacer.DefineLocation(locationAcquisition);
        }

        maudio = GetComponent<AudioSource>();

        //initialize original baked lights
        LightmapData[] lightmaparray = LightmapSettings.lightmaps;
        ambientOriginal = new Texture2D[lightmaparray.Length];

        for (var i = 0; i < lightmaparray.Length; i++)
        {
            ambientOriginal[i] = lightmaparray[i].lightmapColor;
        }

        InitComms();
    }

    // Update is called once per frame
    void Update()
    {
        bool lprimar, rprimar, lsec, rsec;
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primaryButton, out lprimar);
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primaryButton, out rprimar);
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.secondaryButton, out lsec);
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.secondaryButton, out rsec);

        if (lsec && rsec)
        {
            CamShake();
        }

        if (externalCommsRequested && !logging){
            if (!lprimar && !rprimar && !lsec && !rsec)
            {
                buttonLock = false;
            }

            if (externalCommCanvas.transform.GetChild(1).gameObject.active)
            {
                if (Input.GetKeyDown(KeyCode.Y) || Input.GetKeyDown(KeyCode.N) || ( (lprimar||rprimar||lsec||rsec)&&!buttonLock) )
                {
                    externalCommCanvas.transform.GetChild(1).gameObject.SetActive(false);
                    externalCommCanvas.transform.GetChild(2).gameObject.SetActive(true);
                    buttonLock = true;
                }
            }
            else {
                if (Input.GetKeyDown(KeyCode.Y) || ( (lprimar || rprimar) && !buttonLock) ) {
                    externalCommCanvas.transform.GetChild(1).gameObject.SetActive(true);
                    externalCommCanvas.transform.GetChild(2).gameObject.SetActive(true);
                    externalCommCanvas.gameObject.SetActive(false);
                    Time.timeScale = 1f;
                    externalCommsRequested = false;
                    buttonLock = true;
                }
                if (Input.GetKeyDown(KeyCode.N) || ( (lsec || rsec)&& !buttonLock) ) {
                    buttonLock = true;
                    Debug.Log("<color=red>External Comms not ready, exiting...</color>");
                    Application.Quit();
                }
            }
            return;
        }
        
        if (logging) {
            logtimer -= Time.deltaTime;
            if (logtimer <= 0f) {
                Logdata();
                logtimer = 1 / lograte;
            }
        }

        if (colorTransition) {
            if (!smoothLightTransition) {
                transientColor = transientTarget;
                for (int i = 0; i < sceneLights.Count; i++)
                {
                    sceneLights[i].color = transientTarget;
                    //sceneLights[i].intensity = 0f;
                }
                transienttint.a = 0f;
                colorTransition = false;
                tintfx.color = transienttint;

                SendMarks(rns: true);
                Logger.Loginfo(message: "Lights back to normal.");
            }
            else
            {
                colorChangeTimer -= Time.deltaTime;
                transientColor = Color.Lerp(transientColor, transientTarget, 10*colorChangeRate * Time.deltaTime);
                for (int i = 0; i < sceneLights.Count; i++)
                {
                    sceneLights[i].color = transientColor;
                    //sceneLights[i].intensity = colorFXrange * (colorChangeTimer/(1f/colorChangeRate));
                }

                transienttint.a = Mathf.Lerp(transienttint.a, 0f, 4 * colorChangeRate * Time.deltaTime);

                if (colorChangeTimer<=0f)
                {
                    for (int i = 0; i < sceneLights.Count; i++)
                    {
                        sceneLights[i].color = transientTarget;
                        //sceneLights[i].intensity = 0f;
                    }
                    transienttint.a = 0f;
                    colorTransition = false;

                    SendMarks(rns: true);
                    Logger.Loginfo(message: "Lights back to normal.");
                }
                tintfx.color = transienttint;
            }
        }
        if (prepareTimer > 0f)
        {
            prepareTimer -= Time.deltaTime;

            if (gazeDirector) {
                if(fearStims[fear_i].fearStim)
                    gazedir = Vector3.ProjectOnPlane(fearStims[fear_i].fearStim.transform.position - headTransform.position, Vector3.up);
                else
                    gazedir = Vector3.ProjectOnPlane(fearStims[fear_i].TriggerLocation.position - headTransform.position, Vector3.up);

                if (Vector3.Angle(Vector3.ProjectOnPlane(headTransform.forward, Vector3.up), gazedir) > gazetol) {
                    gazeDirector.gameObject.SetActive(true);
                    if (!fearStims[fear_i].fearStim) {
                        gazeDirector.right = -Vector3.Project(gazedir, Vector3.right);
                    }
                    else
                    {
                        if (fearStims[fear_i].fearStim.name.Contains("bat"))
                        {
                            gazeDirector.right = -Vector3.Project(gazedir, Vector3.right);
                        }
                        else
                        {
                            //gazeDirector.right = -Vector3.Dot(gazedir,headTransform.right) * headTransform.right;
                            //gazeDirector.right = Vector3.Dot(gazedir, headTransform.right) * Vector3.right;
                            gazeDirector.right = -gazedir;
                        }
                    }
                }
                else { gazeDirector.gameObject.SetActive(false); }
            }

            if (prepareTimer <= 0f)
            {
                if (spicker)
                {
                    if (spicker.desiredStims[fear_i] == "f" && !practice && taskphase == taskphases.acquisition)
                    {
                        //trigger stim
                        Debug.Log("Activating fear:" + fearStims[fear_i].fearStim.name);
                        fearStims[fear_i].fearStim.SetActive(true);
                    }
                }
                else {
                    if (aplacer.desiredStims[fear_i] == "f" && !practice && taskphase == taskphases.acquisition)
                    {
                        //trigger stim
                        Debug.Log("Activating spider fear.");
                        currentFear = aplacer.PlaceStim(headTransform.position); 
                    }
                }
                joyWalker.frozen = true;                

                if (practice || taskphase == taskphases.extinction) {
                    if(spicker)
                        exposingTimer = spicker.neutralTime;
                    else
                        exposingTimer = aplacer.neutralTime;
                }
                else {
                    if (spicker)
                    {
                        if(desiredStimsAcq[fear_i] == "n")
                            exposingTimer = spicker.neutralTime;
                        else
                            exposingTimer = fearStims[fear_i].expositionTime;
                    }
                    else
                    {
                        if (desiredStimsAcq[fear_i] == "n")
                            exposingTimer = aplacer.neutralTime;
                        else
                            exposingTimer = aplacer.exposingTime;
                    }
                }                
                if (!leaveLightON)
                {
                    transientTarget = ambientLightColor;
                    ChangeBakedLightColor(neutral_light, ToNormal: true);
                    StimSound(Stop:true);
                    colorTransition = true;
                    Logger.Loginfo(message: "started color transition");
                }

                if (gazeDirector)
                    gazeDirector.gameObject.SetActive(false);
                if (fireflies)
                {
                    fireflies.Stop();
                    //fireflies.Clear();
                }
            }
        }        

        if (exposingTimer > 0f) {
            exposingTimer -= Time.deltaTime;
            if (exposingTimer <= 0f) {
                //ended stim
                if (spicker)
                {
                    if (fear_i >= 0 && fear_i < spicker.desiredStims.Length)
                    {
                        if (spicker.desiredStims[fear_i] == "f")
                        {
                            fearStims[fear_i].fearStim.SetActive(false);
                            //reseting bat positions
                            if (fearStims[fear_i].fearStim.name.Contains("batSwarm"))
                            {
                                for (int j = 0; j < 3; j++)
                                {
                                    fearStims[fear_i].fearStim.transform.GetChild(j).GetComponent<BezierFlight>().ResetPos();
                                }
                            }
                        }
                    }
                }
                else {
                    if (fear_i >= 0 && fear_i < aplacer.desiredStims.Length)
                    {
                        if (currentFear)
                        {
                            Destroy(currentFear);
                        }
                    }
                }

                if (spicker) {
                    if (practice || taskphase == taskphases.extinction || !(taskphase == taskphases.acquisition && spicker.desiredStims[fear_i] == "f" && lightChangeOnJumpOut))
                    {
                        transientTarget = ambientLightColor;
                        ChangeBakedLightColor(neutral_light, ToNormal: true);
                        StimSound(Stop: true);
                        colorTransition = true;
                        Logger.Loginfo(message: "started color transition");

                        joyWalker.frozen = false;
                        Logger.Loginfo(message: "Able to walk again.");
                    }
                }
                else {
                    if (practice || taskphase == taskphases.extinction || !(taskphase == taskphases.acquisition && aplacer.desiredStims[fear_i] == "f" && lightChangeOnJumpOut))
                    {
                        transientTarget = ambientLightColor;
                        ChangeBakedLightColor(neutral_light, ToNormal: true);
                        StimSound(Stop: true);
                        colorTransition = true;
                        Logger.Loginfo(message: "started color transition");

                        joyWalker.frozen = false;
                        Logger.Loginfo(message: "Able to walk again.");

                        //timer for next
                        fearTimer = aplacer.desiredITI;
                    }
                }
                
                state = states.walking;
                //SendMarks();
                Logger.Loginfo(message: "Stimulus ended.");
            }
        }

        if (!runningExperiment)
        {
            float ltriggervalue, rtriggervalue;            
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.trigger, out ltriggervalue);
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.trigger, out rtriggervalue);
            

            if ((lsec && lprimar) || (rsec && rprimar) || Input.GetKeyDown(KeyCode.H))
                ExperimentStart();
            if ((ltriggervalue > 0.5f && lprimar) || (rtriggervalue > 0.5f && rprimar) || Input.GetKeyDown(KeyCode.Return))
            {
                ExperimentStart();
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (quitTimer <= 0f)
            {
                quitTimer = quitDelay;
            }
            else
            {
                if (runningExperiment)
                {
                    runningExperiment = false;
                    Logger.terminate();
                }
                Application.Quit();
                Debug.Log("<color=yellow>just quit.</color>");
            }
        }
        if (quitTimer > 0f)
            quitTimer -= Time.deltaTime;


        if(Input.GetKeyDown(KeyCode.R)) {
            QuitEarly();
        }

        if(habitTimer>0f) {
            habitTimer -= Time.deltaTime;

            //check participant speed to warn for movement please
            float personspeed = Vector3.ProjectOnPlane(oldpos - headTransform.position, Vector3.up).magnitude / Time.deltaTime;
            oldpos = headTransform.position;
            userui.text = "";
            if(personspeed <= 0.25f && fadeTimer<=0f)
            {
                timer2move += Time.deltaTime;
                if(timer2move >= time2move)
                {
                    userui.text = "please refrain from stopping";
                }
            }
            else { timer2move = 0f; }

            if (habitTimer <= 0f) {
                habituated = true;
                userui.text = "return to initial position";
                beginSpot.gameObject.SetActive(true);

                if (habitstate == habituates.finalextinction)
                {
                    TaskEnded(false);
                }
            }
        }
        if(habituated){
            //check for auto start task
            if (Vector3.ProjectOnPlane(rater.beginSpot.position - headTransform.position, Vector3.up).magnitude < 0.5f)
            {
                habituated = false;                
                StartAcquisition();
            }
        }
        if(needsHabituation)
        {
            //check for auto start task
            if (Vector3.ProjectOnPlane(rater.beginSpot.position - headTransform.position, Vector3.up).magnitude < 0.5f)
            {
                needsHabituation = false;
                instrGO.SetActive(true);
                HabituationStart();
            }
        }

        if (fadeTimer > 0f)
        {
            fadeTimer -= Time.deltaTime;
            if (fadetarget > 0f)
            {
                fadecolor.a = Mathf.Pow((fadeTime - fadeTimer) / fadeTime, 1);
            }
            else
            {
                fadecolor.a = 1f - Mathf.Pow((fadeTime - fadeTimer) / fadeTime, 2);
            }
            transitionFX.color = fadecolor;
            if (fadeTimer <= 0f)
            {
                fadecolor.a = fadetarget;
                transitionFX.color = fadecolor;
                Debug.Log("finished fade transition");
            }
        }

        if (fearTimer > 0f)
        {
            fearTimer -= Time.deltaTime;
            waiting4incorridor = true;

            if (fearTimer <= 0f && insideCorridors)
            {
                VerifyNSpawnTrigger();
            }
        }
        else {
            if (waiting4incorridor && insideCorridors) {
                VerifyNSpawnTrigger();
            }        
        }

        //enter debug mode
        if (Input.GetKeyDown(KeyCode.Space) && Input.GetKey(KeyCode.LeftControl) && !debugMode) {
            debugMode = true;
            joyWalker.enabled = true;
            joyWalker.forthSpeed *= 10f;
            joyWalker.lateralSpeed *= 10f;
        }
        if(Input.GetKeyDown(KeyCode.M)){
            Logger.Loginfo(message: "[debug] mark"); 
            SendMarks(rns: true); 
        }
        if (debugMode)
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                aplacer.PlaceStim(headTransform.position);
            }

            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                ChangeBakedLightColor(fear_light);
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                ChangeBakedLightColor(neutral_light, ToNormal: true);
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                ChangeBakedLightColor(neutral_light);
            }
            if (Input.GetKeyDown(KeyCode.L)) {
                Debug.Log("Testing neutral light transition");
                if(practice)
                {
                    if (!invertFearColor)
                    {
                        transientColor = neutralPracticeColor;
                        ChangeBakedLightColor(practiceNeutral_light);
                    }
                    else
                    {
                        transientColor = fearPracticeColor;
                        ChangeBakedLightColor(practiceFear_light);
                    }
                }
                else
                {
                    if (!invertFearColor)
                    {
                        transientColor = neutralLightColor;
                        ChangeBakedLightColor(neutral_light);
                    }
                    else
                    {
                        transientColor = fearLightColor;
                        ChangeBakedLightColor(fear_light);
                    }
                }
                for (int i = 0; i < sceneLights.Count; i++)
                {
                    sceneLights[i].color = transientColor;
                    //sceneLights[i].intensity = colorFXrange;
                }
                transienttint = transientColor;
                transienttint.a = tintamount;
                tintfx.color = transienttint;
                state = states.unusualLight;
                colorTransition = false;
                colorChangeTimer = (1f/colorChangeRate);
                exposingTimer = 3f;
            }
            if (Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftShift)){
                Debug.Log("Testing negative light transition");
                if(practice)
                {
                    if (!invertFearColor)
                    {
                        transientColor = fearPracticeColor;
                        ChangeBakedLightColor(practiceFear_light);
                    }
                    else
                    {
                        transientColor = neutralPracticeColor;
                        ChangeBakedLightColor(practiceNeutral_light);
                    }
                }
                else
                {
                    if (!invertFearColor)
                    {
                        transientColor = fearLightColor;
                        ChangeBakedLightColor(fear_light);
                    }
                    else
                    {
                        transientColor = neutralLightColor;
                        ChangeBakedLightColor(neutral_light);
                    }
                }
                for (int i = 0; i < sceneLights.Count; i++)
                {
                    sceneLights[i].color = transientColor;
                    //sceneLights[i].intensity = colorFXrange;
                }
                transienttint = transientColor;
                transienttint.a = tintamount;
                tintfx.color = transienttint;
                state = states.unusualLight;
                colorChangeTimer = (1f/colorChangeRate);
                colorTransition = false;
                exposingTimer = 3f;
            }
            if (Input.GetKeyDown(KeyCode.K)) { CamShake(); }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Finish") {
            Debug.Log("triggered by: "+other.gameObject.name);
            other.gameObject.SetActive(false);
            //if(state == states.walking){
                if (fear_i + 1 < spicker.desiredStims.Length)
                {
                    fear_i++;
                    if (spicker.desiredStims[fear_i]=="f")
                    {
                        if (practice) {
                            statestr = fear_i.ToString() + ": negative practice";
                            ChangeEnvLightColor(practice, Fear: true);
                            Logger.Loginfo(message: "Arrived at the " + fear_i + " negative practice stimulus.");
                            StimSound(NegativeSound: true);
                        }
                        else
                        {
                            if (taskphase == taskphases.extinction) {
                                statestr = fear_i.ToString() + ": negative light" ;
                                ChangeEnvLightColor(practice, Fear: true);                                
                                Logger.Loginfo(message: "Arrived at the " + fear_i + " stimulus.");
                            }
                            else {
                                statestr = fear_i.ToString() + ":" + fearStims[fear_i].fearStim.name;
                                ChangeEnvLightColor(practice, Fear: true);
                                Logger.Loginfo(message: "Arrived at the " + fearStims[fear_i].fearStim.name + " stimulus.");
                            }
                            StimSound(NegativeSound: true);
                        }
                    }
                    else {

                        if (practice)
                        {
                            statestr = fear_i.ToString() + ":neutral stimulus practice";
                            ChangeEnvLightColor(practice, Fear: false);
                            Logger.Loginfo(message: "Arrived at a neutral stimulus practice trigger.");
                        }
                        else {
                            statestr = fear_i.ToString() + ":neutral stimulus";
                            ChangeEnvLightColor(practice, Fear: false);
                            Logger.Loginfo(message: "Arrived at a neutral stimulus trigger.");
                        }
                        StimSound(NegativeSound: false);
                    }
                    prepareTimer = prepareTime;

                    if (gazeDirector)
                        gazeDirector.gameObject.SetActive(true);
                    if (fireflies && usefireflies)
                    {
                        if (spicker.desiredStims[fear_i]=="n")
                        {
                            gazedir = Vector3.ProjectOnPlane(fearStims[fear_i].TriggerLocation.position - headTransform.position, Vector3.up).normalized;
                            fireflies.transform.position = headTransform.position + Vector3.Project(2.0f * gazedir, Vector3.right);
                        }
                        else
                        {
                            gazedir = Vector3.ProjectOnPlane(fearStims[fear_i].fearStim.transform.position - headTransform.position, Vector3.up).normalized;

                            if (fearStims[fear_i].fearStim.name.Contains("bat"))
                                fireflies.transform.position = headTransform.position + Vector3.Project(2.0f * gazedir, Vector3.right);
                            else if (fearStims[fear_i].fearStim.name.Contains("vespa"))
                                fireflies.transform.position = fearStims[fear_i].fearStim.transform.position - 1.5f * fearStims[fear_i].fearStim.transform.forward;
                            else
                                fireflies.transform.position = headTransform.position + 0.5f * (fearStims[fear_i].fearStim.transform.position - headTransform.position);
                        }
                        fireflies.Play();
                    }
                    joyWalker.frozen = true;
                    Logger.Loginfo(message: "Walking blocked.");

                    //activating next trigger
                    if (fear_i + 1 < spicker.desiredStims.Length)
                    {
                        fearStims[fear_i + 1].TriggerLocation.gameObject.SetActive(true);
                    }
                    else {
                        //activate final marker
                        FinalTrigger.gameObject.SetActive(true);

                        if (taskphase == taskphases.acquisition)
                            PlayerPrefs.SetInt("resting", 1);
                        else
                            PlayerPrefs.SetInt("resting", 0);    

                        ratingsgrp.SetActive(true);
                        rater.questiontxt.text = instructions4questions;
                        rater.analogPanel.SetActive(false);
                        rater.digitalPanel.SetActive(false);
                        rater.shadowRater.gameObject.SetActive(false);
                        //rater.NextQuestion();
                        /*
                        if (practice)
                        {
                            rater.Finish();
                        }
                        else
                        {
                            rater.NextQuestion();  
                        }
                        */
                    }
                    SendMarks();
                }
                else {
                    if (practice)
                    {
                        Debug.Log("no more fear!");
                        //Logger.Loginfo(message: "Reached the end of the experiment. End of practice.");
                        //logging = false;
                    }
                    else
                    {
                        Debug.Log("no more fear!");
                        //Logger.Loginfo(message: "Reached the end of the experiment. Started rating.");
                        //logging = false;
                    }
                }
            //}
        }
        else if (other.tag == "InsideCorridors")
        {
            insideCorridors = true;
            if(waiting4incorridor)
            {
                Vector3 nextstimpos = headTransform.position;
                if (taskphase == taskphases.acquisition)
                {
                    nextstimpos = acqStimpos[stim_i];
                }
                else if (taskphase == taskphases.extinction)
                {
                    nextstimpos = extStimpos[stim_i];
                }

                if (Mathf.Abs(headTransform.position.z - nextstimpos.z) < 1.6f)
                    corridorclear = false;
            }
        }
    }
    void OnTriggerExit(Collider other)
    {
        if(other.tag == "InsideCorridors"){
            insideCorridors = false;

            if(!corridorclear && waiting4incorridor) {
                Debug.Log("<color=orange>Stimpos " + stim_i + " skipped </color>");
                stim_i++;
                corridorclear = true;
            }
        }    
    }

    public void ChangeEnvLightColor(bool Practice, bool Fear=false, bool Back2Normal=false) {
        Color TransientColor;
        Texture2D EnvColor;

        if (!Back2Normal) {
            if (Practice) {
                if (!invertFearColor)
                {
                    if (Fear) {
                        TransientColor = fearPracticeColor;
                        EnvColor = practiceFear_light;
                    }
                    else {
                        TransientColor = neutralPracticeColor;
                        EnvColor = practiceNeutral_light;
                    }
                }
                else {
                    if (!Fear)
                    {
                        TransientColor = fearPracticeColor;
                        EnvColor = practiceFear_light;
                    }
                    else
                    {
                        TransientColor = neutralPracticeColor;
                        EnvColor = practiceNeutral_light;
                    }
                }
            }
            else {
                if (!invertFearColor)
                {
                    if (Fear)
                    {
                        TransientColor = fearLightColor;
                        EnvColor = fear_light;
                    }
                    else
                    {
                        TransientColor = neutralLightColor;
                        EnvColor = neutral_light;
                    }
                }
                else
                {
                    if (!Fear)
                    {
                        TransientColor = fearLightColor;
                        EnvColor = fear_light;
                    }
                    else
                    {
                        TransientColor = neutralLightColor;
                        EnvColor = neutral_light;
                    }
                }
            }
            ChangeBakedLightColor(EnvColor);
            for (int i = 0; i < sceneLights.Count; i++)
            {
                sceneLights[i].color = TransientColor;
                //sceneLights[i].intensity = colorFXrange;
            }
            transienttint = TransientColor;
            transienttint.a = tintamount;
            tintfx.color = transienttint;
            state = states.unusualLight;
            colorChangeTimer = (1f / colorChangeRate);
            colorTransition = false;
        }
        else {
            TransientColor = ambientLightColor;
            ChangeBakedLightColor(neutral_light, ToNormal: true);
            for (int i = 0; i < sceneLights.Count; i++)
            {
                sceneLights[i].color = TransientColor;
                //sceneLights[i].intensity = colorFXrange;
            }
        }
    }
    public void CamShake(float shakeGain=1f) {
        joyWalker.CamShake(shakeGain);

        //try to make the controllers vibrate
        UnityEngine.XR.HapticCapabilities capabilitiesL;
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetHapticCapabilities(out capabilitiesL);
        if (capabilitiesL.supportsImpulse)
        {
            uint channel = 0;
            float amplitude = 1.0f;
            float duration = 3.0f;
            InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).SendHapticImpulse(channel, amplitude, duration);
        }
        UnityEngine.XR.HapticCapabilities capabilitiesR;
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetHapticCapabilities(out capabilitiesR);
        if (capabilitiesR.supportsImpulse)
        {
            uint channel = 0;
            float amplitude = 1.0f;
            float duration = 3.0f;
            InputDevices.GetDeviceAtXRNode(XRNode.RightHand).SendHapticImpulse(channel, amplitude, duration);
        }

        if(useRaspberry && raspyVibrate){
            //raspberryComm.sendVibration();
            SlaveThreadRPi.SendVibration();
        }
    }

    public void QuitEarly() {
        //reposition the rating group
        Vector3 ratingpos = headTransform.position;
        //to be about 1.5f from the participant
        ratingpos += Vector3.Dot(headTransform.forward, Vector3.right) * 1.5f * Vector3.right;
        ratingpos.y = 0f;

        if(spicker)
            spicker.ratingsgrp.position = ratingpos;
        else
            aplacer.ratingsgrp.position = ratingpos;

        if (taskphase == taskphases.acquisition)
            PlayerPrefs.SetInt("resting", 1);
        else
            PlayerPrefs.SetInt("resting", 0);

        ratingsgrp.SetActive(true);
        rater.questiontxt.text = instructions4questions;
        //rater.NextQuestion();
    }

    void HabituationStart() {
        userui.text = "";
        beginSpot.gameObject.SetActive(false);
        habituated = false;
        if(habituationSpecific)
            habitTimer = maxHabituationTime;
        else
            habitTimer = 3*maxHabituationTime+7*fadeTime;
        StartCoroutine(HabituationCoroutine());


        Logger.Loginfo(message: "Starting Habituation !!!!!! ");
        SendMarks(rns: true);
        Logger.Loginfo(message: "Start Habituation. MARK.");

    }
    IEnumerator HabituationCoroutine() {
        //connect to raspberries for marks and start logging 
        if (!practice)
        {
            StartLogging();
            InitComms();
        }

        HideDebugInfo();

        //hide optitrack obj
        if (optiObjrender)
            optiObjrender.enabled = false;

        
        if (Vector3.ProjectOnPlane(rater.beginSpot.position - headTransform.position, Vector3.up).magnitude < 0.5f)
        {
            RestartPathEnforcers();
        }


        if (habituationSpecific)
        {
            Debug.Log("<color=cyan>Started acquisition habituation</color>");
            //just do the transition to the acquisition environment
            fadeTimer = fadeTime;
            fadetarget = 1.0f;
            //wait fade
            yield return new WaitForSeconds(fadeTime);


            if (spicker)
                currentLocation = spicker.DefineLocation(locationAcquisition);
            else
                currentLocation = aplacer.DefineLocation(locationAcquisition);

            fadeTimer = fadeTime;
            fadetarget = 0.0f;
        }
        else
        {
            int nextLocation = 1;

            //spends 3 mins in each environment
            fadeTimer = fadeTime;
            fadetarget = 1.0f;
            //wait fade
            yield return new WaitForSeconds(fadeTime);
            //switches to library
            if (spicker)
                currentLocation = spicker.DefineLocation(nextLocation);
            else
                currentLocation = aplacer.DefineLocation(nextLocation);

            fadeTimer = fadeTime;
            fadetarget = 0.0f;
            yield return new WaitForSeconds(fadeTime);
            yield return new WaitForSeconds(maxHabituationTime);

            fadeTimer = fadeTime;
            fadetarget = 1.0f;
            //wait fade
            yield return new WaitForSeconds(fadeTime);
            //switches
            nextLocation = 2;
            if (spicker)
                currentLocation = spicker.DefineLocation(nextLocation);
            else
                currentLocation = aplacer.DefineLocation(nextLocation);
            fadeTimer = fadeTime;
            fadetarget = 0.0f;
            yield return new WaitForSeconds(fadeTime);
            yield return new WaitForSeconds(maxHabituationTime);

            fadeTimer = fadeTime;
            fadetarget = 1.0f;
            //wait fade
            yield return new WaitForSeconds(fadeTime);
            //switches
            nextLocation = 3;
            if (spicker)
                currentLocation = spicker.DefineLocation(nextLocation);
            else
                currentLocation = aplacer.DefineLocation(nextLocation);
            fadeTimer = fadeTime;
            fadetarget = 0.0f;
            yield return new WaitForSeconds(fadeTime);
            yield return new WaitForSeconds(maxHabituationTime);

            //only fade to black if there is need
            if (taskphase == taskphases.extinction)
                nextLocation = locationExtinction;
            else
                nextLocation = locationAcquisition;

            if (currentLocation != nextLocation)
            {
                fadeTimer = fadeTime;
                fadetarget = 1.0f;
                //wait fade
                yield return new WaitForSeconds(fadeTime);

                if (spicker)
                    currentLocation = spicker.DefineLocation(nextLocation);
                else
                    currentLocation = aplacer.DefineLocation(nextLocation);

                fadeTimer = fadeTime;
                fadetarget = 0.0f;
            }
        }
        //Debug.Log("<color=cyan>Finished habituation practice</color>");
        SendMarks(rns: true);
        Logger.Loginfo(message: "End Habituation. MARK.");
    }

    void ExperimentStart() {
        //check playerprefs
        bool resting = false;
        if (PlayerPrefs.HasKey("resting"))
            resting = (PlayerPrefs.GetInt("resting") == 1 ? true : false);

        if (taskday == 2 || resting)
        {
            taskphase = taskphases.extinction;
            StartExtinction();
        }
        else
        {
            HabituationStart();
        }
    }

    void StartAcquisition() {
        //check if user is in the right spot and in the right environment
        if(Vector3.ProjectOnPlane(rater.beginSpot.position-headTransform.position,Vector3.up).magnitude < 0.5f)
        {
            stim_i = 0;
            userui.text = "";
            beginSpot.gameObject.SetActive(false);

            HideDebugInfo();

            if (!practice)
            {
                StartLogging();
                InitComms();                
            }

            runningExperiment = true;

            if(spicker)
                spicker.AlreadyStarted();

            //check playerprefs
            bool resting = false;
            if (PlayerPrefs.HasKey("resting"))
                resting = (PlayerPrefs.GetInt("resting")==1?true:false);

            if (taskday == 2 || resting)
            {
                taskphase = taskphases.extinction;
                StartExtinction();
            }
            else {
                //reset fears
                fear_i = -1;
                
                //reset rater
                rater.ResetNeutral();

                SetInstructions(acquisitionInstructions);
                instrGO.SetActive(true);

                //rater.NextQuestion();
                ratingsgrp.SetActive(false);

                taskphase = taskphases.acquisition;
                if (practice) {
                    if (spicker)
                    {
                        spicker.desiredStims = new string[] { "n", "f", "n" };
                        spicker.stimNum = 3;
                    }
                    else {
                        aplacer.desiredStims = new string[] { "n", "f", "n" };
                        aplacer.stimNum = 3;
                    }
                }
                else
                {
                    if(spicker) {
                        spicker.desiredStims = desiredStimsAcq;
                        spicker.stimNum = stimNumAcquisition;
                        spicker.negativeStims = negStimsAcquisition;
                    }
                    else {
                        aplacer.desiredStims = desiredStimsAcq;
                        aplacer.stimNum = stimNumAcquisition;
                        aplacer.negativeStims = negStimsAcquisition;
                    }
                }

                if(spicker) {
                    int pickedCounter = 1;
                    bool skippedEdgeCorridor = spicker.PickStims(this);
                    while (skippedEdgeCorridor && pickedCounter < 20) {
                        pickedCounter++;
                        skippedEdgeCorridor = spicker.PickStims(this);
                    }
                    if (desiredStimsAcq[0] == "f")
                        statestr = "0:" + fearStims[0].fearStim.name;
                    else
                        statestr = "0:neutral stimulus";

                    if (pickedCounter >= 20)
                        Debug.Log("<color=red>PickStims function was not able to satisfy distance requirement without skipping edge corridors. It is highly recommended to try again.</color>");
                }
                Logger.Loginfo(message: "Starting acquisition. MARK");
                SendMarks(rns: true);
            }

            RestartPathEnforcers();
            
            if (spicker)
            {
                //activating first trigger
                fearStims[0].TriggerLocation.gameObject.SetActive(true);
            }
            else {
                //start spider placer timer
                fearTimer = aplacer.desiredITI;
            }

            statestr = "0: First trigger active";
        }
        else {
            //ask user to go to starting spot
            userui.text = "return to initial position";
            beginSpot.gameObject.SetActive(true);
            
            Debug.Log("<color=cyan>not in the right pos</color>");
        }
    }
    void StartLogging() {
        if (!logging && !practice)
        {
            statestr = "0: Habituation";
            
            //open logfile
            Logger.init("PTSD_VR");
            logging = true;
            ReadConfigFile();
        }
    }

    public void CloseComms() {
        logging = false;
        if (useRaspberry)
        {
            //raspberryComm.sendSsr();
            //raspberryComm.closeRPi();
            SlaveThreadRPi.CloseComms();
        }
        if (useSecRaspberry)
        {
            //raspberrySecComm.sendSsr();
            //raspberrySecComm.closeRPi();
        }
    }
    void Logdata() {
        if (logging) {
            if (optitrackObj) {
                Logger.Loginfo(apptime: Time.time, state: statestr + " fearActive:" + (exposingTimer > 0f ? "yes" : "no"),
                        posx: headTransform.position.x,
                        posy: headTransform.position.y,
                        posz: headTransform.position.z,
                        rotx: headTransform.rotation.eulerAngles.x,
                        roty: headTransform.rotation.eulerAngles.y,
                        rotz: headTransform.rotation.eulerAngles.z,
                        optiposx: optitrackObj.position.x,
                        optiposy: optitrackObj.position.y,
                        optiposz: optitrackObj.position.z,
                        optirotx: optitrackObj.rotation.eulerAngles.x,
                        optiroty: optitrackObj.rotation.eulerAngles.y,
                        optirotz: optitrackObj.rotation.eulerAngles.z
                        );
            }
            else
            {
                Logger.Loginfo(apptime: Time.time, state: statestr + " fearActive:" + (exposingTimer > 0f ? "yes" : "no"),
                        posx: headTransform.position.x,
                        posy: headTransform.position.y,
                        posz: headTransform.position.z,
                        rotx: headTransform.rotation.eulerAngles.x,
                        roty: headTransform.rotation.eulerAngles.y,
                        rotz: headTransform.rotation.eulerAngles.z);
            }
        }
    }
    public void SendMarks(bool rns=false) {
        if(!practice){
            if (rns) Debug.Log("<color=yellow>rns mark!</color>");
            else Debug.Log("<color=yellow>normal mark!</color>");

            if (useRaspberry && rns)
            {
                //Debug.Log("sending Raspberry mark");
                Logger.Loginfo(message: "Sending Raspberry mark");
                //raspberryComm.sendMark();
                new Thread(() => {
                    SlaveThreadRPi.SendMark();
                }).Start();
            }
            if (useSecRaspberry)
            {
                //Debug.Log("sending second Raspberry mark");
                Logger.Loginfo(message: "Sending Second Raspberry mark");
                //raspberrySecComm.sendMark();

            }
        }
    }
    void ReadConfigFile() {
        externalCommsRequested=false;
        string externalCommstr = "External comms detected in the cfg:\n";

        SphereRater ratingscript = ratingsgrp.transform.GetChild(0).GetChild(0).GetComponent<SphereRater>();

        string fullpath = Path.Combine(Application.streamingAssetsPath,"config.cfg");
        
        #if UNITY_ANDROID && !UNITY_EDITOR
            fullpath = Path.Combine(Application.persistentDataPath,"config.cfg");
            //extract from the streaming assets for android
            if(!File.Exists(fullpath)){
                //copy config file to persistent android folder                
                string packedfullpath = Path.Combine(Application.streamingAssetsPath, "config.cfg");
                UnityEngine.Networking.UnityWebRequest cfgFile = UnityEngine.Networking.UnityWebRequest.Get(packedfullpath);
                cfgFile.SendWebRequest();
                while (!cfgFile.isDone)
                {//waiting download
                }
                //it is ok to do it in one go because the file is small
                File.WriteAllBytes(Application.persistentDataPath + "/config.cfg", cfgFile.downloadHandler.data);
            }
        #endif

        bool readingAqStimPositions = false;
        bool readingExStimPositions = false;

        acqStimpos = new List<Vector3>();
        extStimpos = new List<Vector3>();

        string cfgline;
        string rawvalue="";
        cfgstream = File.Open(fullpath, FileMode.Open);
        cfgreader = new StreamReader(cfgstream);

        //Debug.Log(fullpath);

        //extract headers
        cfgline = cfgreader.ReadLine();
        //start reading the meat
        cfgline = cfgreader.ReadLine();
        while (cfgline!=null) {
            if(cfgline!="" && !readingAqStimPositions && !readingExStimPositions) {
                if (cfgline.Contains("|"))
                    rawvalue = cfgline.Split('|')[0].Split('=')[1];
                else
                    rawvalue = cfgline.Split('=')[1];
            }

            if (cfgline.ToLower().Contains("taskday"))
            {
                taskday = 1;
                if (!int.TryParse(rawvalue, out taskday))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the taskday");                

                Logger.Loginfo(message: "[cfg] taskday:" + taskday);
            }
            else if (cfgline.ToLower().Contains("invertfearcolor"))
            {
                int invcolor_int = 0;
                if (!int.TryParse(rawvalue, out invcolor_int))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the invertFearColor");
                else
                    invertFearColor = invcolor_int == 1 ? true : false;

                Logger.Loginfo(message: "[cfg] invertFearColor:" + invertFearColor);
            }
            else if (cfgline.ToLower().Contains("fireflies"))
            {
                int useffl_int = 0;
                if (!int.TryParse(rawvalue, out useffl_int))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the fireflies");
                else
                    usefireflies = useffl_int == 1 ? true : false;

                Logger.Loginfo(message: "[cfg] fireflies:" + usefireflies);
            }
            else if (cfgline.ToLower().Contains("gazedirector"))
            {
                int usegd_int = 0;
                if (!int.TryParse(rawvalue, out usegd_int))
                {
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the gazeDirector");
                }
                else
                {
                    if (usegd_int == 0)
                        gazeDirector = null;
                }
                Logger.Loginfo(message: "[cfg] Use gazeDirector:" + usegd_int);
            }
            else if (cfgline.ToLower().Contains("lograte"))
            {
                if (!float.TryParse(rawvalue, out lograte))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the lograte");
                Logger.Loginfo(message: "[cfg] lograte:" + lograte);
            }
            else if (cfgline.ToLower().Contains("preparetime"))
            {
                if (!float.TryParse(rawvalue, out prepareTime))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the prepareTime");
                Logger.Loginfo(message: "[cfg] PrepareTime:" + prepareTime);
            }
            else if (cfgline.ToLower().Contains("locationacquisition"))
            {
                if (!int.TryParse(rawvalue, out locationAcquisition))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the locationAcquisition");
                Logger.Loginfo(message: "[cfg] Location4acquisition:" + locationAcquisition);
            }
            else if (cfgline.ToLower().Contains("locationextinction"))
            {
                if (!int.TryParse(rawvalue, out locationExtinction))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the locationExtinction");
                Logger.Loginfo(message: "[cfg] Location4extinction:" + locationExtinction);
            }
            else if (cfgline.ToLower().Contains("ratingpanelheight"))
            {
                float ratingPanelHeight = 1.2f;
                if (!float.TryParse(rawvalue, out ratingPanelHeight))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the ratingPanelHeight");
                ratingsgrp.transform.GetChild(0).localPosition = new Vector3(0f, ratingPanelHeight, 0f);
                if (instrGO)
                {
                    instrGO.transform.position = new Vector3(instrGO.transform.position.x, ratingPanelHeight, instrGO.transform.position.z);
                }
                Logger.Loginfo(message: "[cfg] RatingPanelHeight:" + ratingPanelHeight);
            }
            else if (cfgline.ToLower().Contains("habituationtime"))
            {
                if (!float.TryParse(rawvalue, out maxHabituationTime))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the habituationTime");
                Logger.Loginfo(message: "[cfg] HabituationTime:" + maxHabituationTime);
            }
            else if (cfgline.ToLower().Contains("habituationallenvs"))
            {
                int tempHabspec = 0;
                if (!int.TryParse(rawvalue, out tempHabspec))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the habituationAllEnvs");
                Logger.Loginfo(message: "[cfg] habituationAllEnvs:" + tempHabspec);
                habituationSpecific = tempHabspec < 1;
            }
            else if (cfgline.ToLower().Contains("desirediti"))
            {
                if (aplacer)
                {
                    if (!float.TryParse(rawvalue, out aplacer.desiredITI))
                        Debug.Log("[FearTaskManager cfg reader] There was a problem reading the desiredITI");

                    Logger.Loginfo(message: "[cfg] desiredITI:" + aplacer.desiredITI);
                }
            }
            else if (cfgline.ToLower().Contains("useraspberry"))
            {
                int userasp_int = 0;
                if (!int.TryParse(rawvalue, out userasp_int))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the useRaspberry");
                else
                    useRaspberry = (userasp_int == 1 ? true : false);
                //Debug.Log("just set userasp to: " + userasp_int);
                Logger.Loginfo(message: "[cfg] UseRaspberry:" + userasp_int);
                externalCommsRequested = useRaspberry || externalCommsRequested;
                if (useRaspberry)
                    externalCommstr += "useRaspberry\n";
            }
            else if (cfgline.ToLower().Contains("raspberryip"))
            {
                raspberryIP = rawvalue;
                Logger.Loginfo(message: "[cfg] RaspberryIP:" + raspberryIP);
            }
            else if (cfgline.ToLower().Contains("raspberryport"))
            {
                int rnsRaspberryPort_int = 0;
                if (!int.TryParse(rawvalue, out rnsRaspberryPort_int))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the RaspberryPort");
                raspberryPort = rnsRaspberryPort_int;
                Logger.Loginfo(message: "[cfg] RaspberryPort:" + raspberryPort);
            }
            else if (cfgline.ToLower().Contains("usesecraspberry"))
            {
                int userasp_int = 0;
                if (!int.TryParse(rawvalue, out userasp_int))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the useSecRaspberry");
                else
                    useSecRaspberry = (userasp_int == 1 ? true : false);
                //Debug.Log("just set userasp to: " + userasp_int);
                Logger.Loginfo(message: "[cfg] Use SecRaspberry:" + useSecRaspberry);
                externalCommsRequested = useSecRaspberry || externalCommsRequested;
                if (useSecRaspberry)
                    externalCommstr += "useSecRaspberry\n";
            }
            else if (cfgline.ToLower().Contains("raspberrysecip"))
            {
                raspberrySecIP = rawvalue;
                Logger.Loginfo(message: "[cfg] SecRaspberryIP:" + raspberrySecIP);
            }
            else if (cfgline.ToLower().Contains("raspberrysecport"))
            {
                int rnsRaspberryPort_int = 0;
                if (!int.TryParse(rawvalue, out rnsRaspberryPort_int))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the secRaspberryPort");
                raspberrySecPort = rnsRaspberryPort_int;
                Logger.Loginfo(message: "[cfg] SecRaspberryPort:" + rnsRaspberryPort_int);
            }
            else if (cfgline.ToLower().Contains("raspyvibrate"))
            {
                int raspyv = 0;
                if (!int.TryParse(rawvalue, out raspyv))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the raspyVibrate");
                raspyVibrate = (raspyv==1?true:false);
                Logger.Loginfo(message: "[cfg] raspyVibrate:" + raspyVibrate);
            }
            else if (cfgline.ToLower().Contains("stimnumberacquisition"))
            {
                string[] acqstims = rawvalue.Split(',');
                desiredStimsAcq = acqstims;
                stimNumAcquisition = acqstims.Length;
                int neutralDesiredStims = 0;
                //counting number of neutrals desired
                for (int i = 0; i < acqstims.Length; i++)
                {
                    if (acqstims[i] == "n")
                        neutralDesiredStims++;
                }
                negStimsAcquisition = stimNumAcquisition - neutralDesiredStims;
                Debug.Log("<color=yellow>Acquisition Desired stims:" + stimNumAcquisition + " fears:" + negStimsAcquisition + " neutrals:" + neutralDesiredStims + "</color>");
                Logger.Loginfo(message: "[cfg] DesiredAcquisitionStims:" + stimNumAcquisition + ":" + rawvalue);
            }
            else if (cfgline.ToLower().Contains("stimnumberextinction"))
            {
                string[] extstims = rawvalue.Split(',');
                stimNumExtinction = extstims.Length;
                desiredStimsExt = extstims;
                Logger.Loginfo(message: "[cfg] DesiredExtinctionStims:" + stimNumExtinction + ":" + rawvalue);
            }
            else if (cfgline.ToLower().Contains("onlynegativestimuli"))
            {
                int usettl_int = 0;
                if (!int.TryParse(rawvalue, out usettl_int))
                {
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the onlyNegativeStimuli");
                }
                else
                {
                    if (spicker)
                    {
                        spicker.negativeOnly = usettl_int == 1 ? true : false;
                        Logger.Loginfo(message: "[cfg] onlyNegativeStims:" + spicker.negativeOnly);
                    }
                }
            }
            else if (cfgline.ToLower().Contains("neutraltime"))
            {
                if (spicker)
                {
                    if (!float.TryParse(rawvalue, out spicker.neutralTime))
                        Debug.Log("[FearTaskManager cfg reader] There was a problem reading the neutralTime");
                    Logger.Loginfo(message: "[cfg] neutralTime:" + spicker.neutralTime);
                }
                else
                {
                    if (!float.TryParse(rawvalue, out aplacer.neutralTime))
                        Debug.Log("[FearTaskManager cfg reader] There was a problem reading the neutralTime");
                    Logger.Loginfo(message: "[cfg] neutralTime:" + aplacer.neutralTime);
                }
            }
            else if (cfgline.ToLower().Contains("relaxtime"))
            {
                if (!float.TryParse(rawvalue, out ratingscript.relaxTime))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the relaxTime");
                Logger.Loginfo(message: "[cfg] RelaxTime:" + ratingscript.relaxTime);
            }
            else if (cfgline.ToLower().Contains("ratingpanelminimumlookdistance"))
            {
                if (!float.TryParse(rawvalue, out ratingscript.lookdist))
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the ratingPanelMinimumLookDistance");
                Logger.Loginfo(message: "[cfg] RatingPanelMinimumLookDistance:" + ratingscript.lookdist);
            }
            else if (cfgline.ToLower().Contains("ratingtaskquestions"))
            {
                rawvalue = rawvalue.Replace("\\n", "\n");
                ratingscript.TaskQuestions = rawvalue.Split(',');
                ratingscript.InvertFearColors(invertFearColor);
                Logger.Loginfo(message: "[cfg] ratingTaskQuestions:" + rawvalue);
            }
            else if (cfgline.ToLower().Contains("instructions4habituation"))
            {
                habituationInstructions = rawvalue.Replace("\\n", "\n");
                Logger.Loginfo(message: "[cfg] Habituation instructions:" + habituationInstructions);
                SetInstructions(habituationInstructions);
            }
            else if (cfgline.ToLower().Contains("instructions4finalhabituation"))
            {
                finalHabituationInstructions = rawvalue.Replace("\\n", "\n");
                Logger.Loginfo(message: "[cfg] finalHabituation Instructions:" + finalHabituationInstructions);
            }
            else if (cfgline.ToLower().Contains("instructions4acquisition"))
            {
                acquisitionInstructions = rawvalue.Replace("\\n", "\n");
                Logger.Loginfo(message: "[cfg] AcquisitionInstructions:" + acquisitionInstructions);
            }
            else if (cfgline.ToLower().Contains("instructions4extinction"))
            {
                extinctionInstructions = rawvalue.Replace("\\n", "\n");
                Logger.Loginfo(message: "[cfg] Extinction instructions:" + extinctionInstructions);
            }
            else if (cfgline.ToLower().Contains("instructions4questions"))
            {
                instructions4questions = rawvalue;
                Logger.Loginfo(message: "[cfg] Questions instructions:" + instructions4questions);
            }
            else if (cfgline.ToLower().Contains("triggerdist")) {
                triggerDist = 2.0f;
                if (!float.TryParse(rawvalue, out triggerDist))
                {
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the triggerDist");
                }
                Logger.Loginfo(message: "[cfg] TriggerDist: " + triggerDist);
            }
            else if (cfgline.ToLower().Contains("acqstimpos=start")) { readingAqStimPositions = true; }
            else if (cfgline.ToLower().Contains("acqstimpos=end")) { readingAqStimPositions = false; }
            else if (cfgline.ToLower().Contains("extstimpos=start")) { readingExStimPositions = true; }
            else if (cfgline.ToLower().Contains("extstimpos=end")) { readingExStimPositions = false; }
            else if (readingAqStimPositions)
            {
                float newx, newy, newz;
                Vector3 newpos;

                if (!float.TryParse(cfgline.Split(' ')[0], out newx))
                {
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the x coordinate for the Acquisition stimpos number " + (acqStimpos.Count + 1));
                }
                if (!float.TryParse(cfgline.Split(' ')[1], out newy))
                {
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the y coordinate for the Acquisition stimpos number " + (acqStimpos.Count + 1));
                }
                if (!float.TryParse(cfgline.Split(' ')[2], out newz))
                {
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the z coordinate for the Acquisition stimpos number " + (acqStimpos.Count + 1));
                }
                newpos = new Vector3(newx, newy, newz);
                acqStimpos.Add(newpos);
                Logger.Loginfo(message: "[cfg] Acquisition stimpos: " + acqStimpos.Count + ": " + newpos);
            }
            else if (readingExStimPositions)
            {
                float newx, newy, newz;
                Vector3 newpos;

                if (!float.TryParse(cfgline.Split(' ')[0], out newx))
                {
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the x coordinate for the Extinction stimpos number " + (extStimpos.Count + 1));
                }
                if (!float.TryParse(cfgline.Split(' ')[1], out newy))
                {
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the y coordinate for the Extinction stimpos number " + (extStimpos.Count + 1));
                }
                if (!float.TryParse(cfgline.Split(' ')[2], out newz))
                {
                    Debug.Log("[FearTaskManager cfg reader] There was a problem reading the z coordinate for the Extinction stimpos number " + (extStimpos.Count + 1));
                }
                newpos = new Vector3(newx, newy, newz);
                extStimpos.Add(newpos);
                Logger.Loginfo(message: "[cfg] Extinction stimpos: " + extStimpos.Count + ": " + newpos);
            }
            cfgline = cfgreader.ReadLine();
        }
        if (externalCommsRequested && !logging) {
            externalCommCanvas.gameObject.SetActive(true);
            externalCommCanvas.transform.GetChild(1).GetComponent<Text>().text = externalCommstr;
            externalCommCanvas.transform.GetChild(1).gameObject.SetActive(true);
            externalCommCanvas.transform.GetChild(2).gameObject.SetActive(false);
        }
        else {
            externalCommCanvas.gameObject.SetActive(false);
        }
        //close file
        cfgreader.Close();
        cfgstream.Close();
    }

    public void SwitchBackNormalLight() {
        if(lightChangeOnJumpOut) {
            Logger.Loginfo(message: "Stim jumped off face.");

            transientTarget = ambientLightColor;
            ChangeBakedLightColor(neutral_light, ToNormal: true);
            StimSound(Stop: true);
            colorTransition = true;
            Logger.Loginfo(message: "started color transition");

            joyWalker.frozen = false;
            Logger.Loginfo(message: "Able to walk again.");

            if(aplacer)
                fearTimer = aplacer.desiredITI;
        }
    }
    public void StartExtinction() {
        //check if user is in the right spot
        if (Vector3.ProjectOnPlane(rater.beginSpot.position - headTransform.position, Vector3.up).magnitude < 0.5f)
        {
            if (!practice)
            {
                StartLogging();
                InitComms();
            }
            runningExperiment = true;

            stim_i = 0;
            userui.text = "";
            beginSpot.gameObject.SetActive(false);
            Logger.Loginfo(message: "Starting extinction.");
            SendMarks(rns: true);

            taskphase = taskphases.extinction;

            if (practice){
                if (spicker)
                {
                    spicker.desiredStims = new string[] { "f", "n", "f" };
                    spicker.stimNum = 3;
                }
                else {
                    aplacer.desiredStims = new string[] { "f", "n", "f" };
                    aplacer.stimNum = 3;
                }
            }
            else {
                if (spicker)
                {
                    spicker.stimNum = stimNumExtinction;
                    spicker.desiredStims = desiredStimsExt;
                }
                else {
                    aplacer.stimNum = stimNumExtinction;
                    aplacer.desiredStims = desiredStimsExt;
                }
            }

            if(spicker)
                currentLocation = spicker.DefineLocation(locationExtinction);
            else
                currentLocation = aplacer.DefineLocation(locationExtinction);

            if (spicker)
            {
                int pickedCounter = 1;
                bool skippedEdgeCorridor = spicker.PickStims(this, extinction: true);
                while (skippedEdgeCorridor && pickedCounter < 20)
                {
                    pickedCounter++;
                    skippedEdgeCorridor = spicker.PickStims(this, extinction: true);
                }

                if (desiredStimsExt[0] == "f")
                    statestr = "0:" + fearStims[0].fearStim.name;
                else
                    statestr = "0:neutral stimulus";

                if (pickedCounter >= 20)
                    Debug.Log("<color=red>PickStims function was not able to satisfy distance requirement without skipping edge corridors. It is highly recommended to try again.</color>");

                //activating first trigger
                fearStims[0].TriggerLocation.gameObject.SetActive(true);
            }
            else{
                fearTimer = aplacer.desiredITI;
            }
            statestr = "0: First trigger active";

            HideDebugInfo();
            RestartPathEnforcers();

            //reset fears
            fear_i = -1;

            //resume logging
            //logging = true;

            //reset rater
            rater.ResetNeutral();

            SetInstructions(extinctionInstructions);

            //rater.NextQuestion();
            ratingsgrp.SetActive(false);
        }
        else {
            //ask user to reposition
            userui.text = "return to initial position";
            beginSpot.gameObject.SetActive(true);
            
            Debug.Log("<color=cyan>not in the right pos</color>");
        }
    }

    void InitComms() {
        if (!commstarted && !practice)
        {
            if (useRaspberry)
            {
                //raspberryComm = new RPi(host: raspberryIP, port: raspberryPort);
                SlaveThreadRPi.init();
                //test stim
                //raspberryComm.sendStim();
                Logger.Loginfo(message: "RNS raspberry comm established");
            }
            if (useSecRaspberry)
            {
                //raspberrySecComm = new RPi(host: raspberrySecIP, port: raspberrySecPort);
                //test stim
                //raspberrySecComm.sendStim();
                //Logger.Loginfo(message: "RNS second raspberry comm established");
            }
            //StartCoroutine(ConfirmationMark());
            commstarted = true;

            //hide optitrack obj
            if (optiObjrender)
                optiObjrender.enabled = false;

            if (optitrackObj && !practice)
                optitrackObj.GetComponent<MotiveExample>().ConnectFun();
        }
    }
    IEnumerator ConfirmationMark() {
        //waits for 10 second after establishing communication
        yield return new WaitForSeconds(10.0f);
        //then sends a mark
		Logger.Loginfo(message: "Sending confirmation mark");
        SendMarks(rns:true);
    }
    public void GetReady4Extinction() {        
        habitTimer = maxHabituationTime+fadeTime;
        habitstate = habituates.acquisition2extinction;
        habituated = false;
        
    }
    public void ResetTask() {
        //to get ready for a full run after the practice        
        runningExperiment = false;
        habitstate = habituates.extinction2acquisition;
        if(logging)
        {
            string fulllogname = Logger.terminate();
            Logger.reinit(fulllogname);
        }
        if (!practice)
        {
            habitstate = habituates.finalextinction;

            bool resting = false;
            if (PlayerPrefs.HasKey("resting"))
                resting = (PlayerPrefs.GetInt("resting") == 1 ? true : false);

            if(taskday == 2)
            {
                //preparing for final habituation
                userui.text = "return to initial position";
                SetInstructions(finalHabituationInstructions);
                beginSpot.gameObject.SetActive(true);
                needsHabituation = true;
                //HabituationStart();
            }
            else{
                if(resting)
                    TaskEnded(true);
                else
                    TaskEnded(false);
            }
        }
        else
        {
            practice = false;
            Debug.Log("<color=cyan>reset task was called</color>");
            //for auto terminate and auto acquisition habituation
            if(habituationSpecific)
                habitTimer = maxHabituationTime+fadeTime;
            else
                habitTimer = 3*maxHabituationTime+7*fadeTime;
        }
    }
    public void StimSound(bool NegativeSound=false, bool Stop=false) {
        if (maudio)
        {
            if (Stop) { maudio.Stop(); }
            else
            {
                if (NegativeSound)
                { maudio.clip = negativeStimSound; }
                else { maudio.clip = neutralStimSound; }

                maudio.loop = true;
                maudio.Play();
            }
        }
    }
    void TaskEnded(bool gorest=true) {
        //task ended
        //Logger.terminate();
        if(gorest)
            userui.text = "rest";
        else
            userui.text = "Thank you";
        Debug.Log("<color=cyan>Task is finished.</color>");
    }

    void RestartPathEnforcers() {
        for (int i = 0; i < initialPathEnforcersActive.Count; i++) { 
            initialPathEnforcersActive[i].SetActive(true); 
        }
        for (int i = 0; i < initialPathEnforcersInactive.Count; i++) { 
            initialPathEnforcersInactive[i].SetActive(false); 
        }
    }
    void HideDebugInfo() {
        for (int i = 0; i < debugElements.Count; i++)
        {
            debugElements[i].gameObject.SetActive(false);
        }
        headTransform.GetComponentInParent<EyetrackHook>().debugMode = false;
    }

    void ActivateAutoLight() {
        //equivalent to triggering a light
        SendMarks();

        float possiblerateDist = 1.2f; //must be bigger than 1, because that is lookdist, the distance that deactivates the auto look in the sphere rater script so the rating panel faces the participant

        if (taskphase == taskphases.extinction)
        {//extinction
            if (fear_i+1 < desiredStimsExt.Length)
            {
                fear_i++;
                if (desiredStimsExt[fear_i] == "f")
                {
                    statestr = fear_i.ToString() + ": negative light";
                    ChangeEnvLightColor(practice, Fear: true);
                    Logger.Loginfo(message: "Arrived at " + statestr);
                    StimSound(NegativeSound: true);
                }
                else
                { //neutral
                    statestr = fear_i.ToString() + ": neutral light";
                    ChangeEnvLightColor(practice, Fear: false);
                    Logger.Loginfo(message: "Arrived at " + statestr);
                    StimSound(NegativeSound: false);
                }
                prepareTimer = prepareTime;

                if (gazeDirector)
                    gazeDirector.gameObject.SetActive(true);
                if (fireflies && usefireflies)
                {
                    if (spicker.desiredStims[fear_i] == "n")
                    {
                        gazedir = Vector3.ProjectOnPlane(fearStims[fear_i].TriggerLocation.position - headTransform.position, Vector3.up).normalized;
                        fireflies.transform.position = headTransform.position + Vector3.Project(2.0f * gazedir, Vector3.right);
                    }
                    else
                    {
                        gazedir = Vector3.ProjectOnPlane(fearStims[fear_i].fearStim.transform.position - headTransform.position, Vector3.up).normalized;

                        if (fearStims[fear_i].fearStim.name.Contains("bat"))
                            fireflies.transform.position = headTransform.position + Vector3.Project(2.0f * gazedir, Vector3.right);
                        else if (fearStims[fear_i].fearStim.name.Contains("vespa"))
                            fireflies.transform.position = fearStims[fear_i].fearStim.transform.position - 1.5f * fearStims[fear_i].fearStim.transform.forward;
                        else
                            fireflies.transform.position = headTransform.position + 0.5f * (fearStims[fear_i].fearStim.transform.position - headTransform.position);
                    }
                    fireflies.Play();
                }
                joyWalker.frozen = true;
                Logger.Loginfo(message: "Walking blocked.");
            }
            else
            {//spawn questions
                Vector3 ratingpos = headTransform.position;
                if (Vector3.Angle(headTransform.forward, Vector3.right) < 90f)
                {
                    ratingpos += possiblerateDist* Vector3.right;
                }
                else
                {
                    ratingpos += -possiblerateDist * Vector3.right;
                }
                ratingpos.y = 0f;
                ratingsgrp.transform.position = ratingpos;

                if (taskphase == taskphases.acquisition)
                    PlayerPrefs.SetInt("resting", 1);
                else
                    PlayerPrefs.SetInt("resting", 0);
                ratingsgrp.SetActive(true);
                rater.questiontxt.text = instructions4questions;
                rater.analogPanel.SetActive(false);
                rater.digitalPanel.SetActive(false);
                rater.shadowRater.gameObject.SetActive(false);
            }
        }
        else
        { //acquisition
            if (fear_i+1 < desiredStimsAcq.Length)
            {
                fear_i++;
                if (desiredStimsAcq[fear_i] == "f")
                {
                    statestr = fear_i.ToString() + ": negative light";
                    ChangeEnvLightColor(practice, Fear: true);
                    Logger.Loginfo(message: "Arrived at " + statestr);
                    StimSound(NegativeSound: true);
                }
                else
                {
                    statestr = fear_i.ToString() + ": neutral light";
                    ChangeEnvLightColor(practice, Fear: false);
                    Logger.Loginfo(message: "Arrived at " + statestr);
                    StimSound(NegativeSound: false);
                }
                prepareTimer = prepareTime;

                if (gazeDirector)
                    gazeDirector.gameObject.SetActive(true);
                if (fireflies && usefireflies)
                {
                    if (spicker.desiredStims[fear_i] == "n")
                    {
                        gazedir = Vector3.ProjectOnPlane(fearStims[fear_i].TriggerLocation.position - headTransform.position, Vector3.up).normalized;
                        fireflies.transform.position = headTransform.position + Vector3.Project(2.0f * gazedir, Vector3.right);
                    }
                    else
                    {
                        gazedir = Vector3.ProjectOnPlane(fearStims[fear_i].fearStim.transform.position - headTransform.position, Vector3.up).normalized;

                        if (fearStims[fear_i].fearStim.name.Contains("bat"))
                            fireflies.transform.position = headTransform.position + Vector3.Project(2.0f * gazedir, Vector3.right);
                        else if (fearStims[fear_i].fearStim.name.Contains("vespa"))
                            fireflies.transform.position = fearStims[fear_i].fearStim.transform.position - 1.5f * fearStims[fear_i].fearStim.transform.forward;
                        else
                            fireflies.transform.position = headTransform.position + 0.5f * (fearStims[fear_i].fearStim.transform.position - headTransform.position);
                    }
                    fireflies.Play();
                }
                joyWalker.frozen = true;
                Logger.Loginfo(message: "Walking blocked.");
            }
            else
            { //spawn questions
                Vector3 ratingpos = headTransform.position;
                if (Vector3.Angle(headTransform.forward, Vector3.right) < 90f)
                {
                    ratingpos += possiblerateDist * Vector3.right;
                }
                else {
                    ratingpos += -possiblerateDist * Vector3.right;
                }
                ratingpos.y = 0f;
                ratingsgrp.transform.position = ratingpos;

                if (taskphase == taskphases.acquisition)
                    PlayerPrefs.SetInt("resting", 1);
                else
                    PlayerPrefs.SetInt("resting", 0);

                ratingsgrp.SetActive(true);
                rater.questiontxt.text = instructions4questions;
                rater.analogPanel.SetActive(false);
                rater.digitalPanel.SetActive(false);
                rater.shadowRater.gameObject.SetActive(false);
            }
        }
    }
    void SetInstructions(string InstructionsToUse) {
        if (instrGO)
        {
            Text instrText = instrGO.transform.GetChild(1).GetComponent<Text>();
            instrText.text = InstructionsToUse;
        }
        else
        {
            Debug.Log("[FearTaskManager cfg reader] Could not find the Instructions panel in the scene.");
        }
    }
    void VerifyNSpawnTrigger() {
        //check if they are close to the next stim pos
        if (taskphase == taskphases.acquisition)
        {
            if (stim_i < acqStimpos.Count)
            {
                if (Vector3.ProjectOnPlane(headTransform.position - acqStimpos[stim_i], Vector3.up).magnitude < triggerDist && !corridorclear)
                {
                    corridorclear = true;
                    stim_i++;
                    Debug.Log("<color=blue>Triggered stim " + stim_i + " </color>");
                    waiting4incorridor = false;
                    ActivateAutoLight();
                }
            }
            else
            {
                Debug.Log("<color=orange>No more stimpos, spawning according to ITI only.</color>");
                //already used all stim positions defined in the cfg, so spawn freely
                waiting4incorridor = false;
                ActivateAutoLight();
            }
        }
        else
        {
            if (stim_i < extStimpos.Count)
            {
                if (Vector3.ProjectOnPlane(headTransform.position - extStimpos[stim_i], Vector3.up).magnitude < triggerDist && !corridorclear)
                {
                    corridorclear = true;
                    stim_i++;
                    Debug.Log("<color=blue>Triggered stim " + stim_i + " </color>");
                    waiting4incorridor = false;
                    ActivateAutoLight();
                }
            }
            else
            {
                Debug.Log("<color=orange>No more stimpos, spawning according to ITI only.</color>");
                //already used all stim positions defined in the cfg, so spawn freely
                waiting4incorridor = false;
                ActivateAutoLight();
            }
        }
    }
    void OnApplicationQuit()
    {
        //close log file
        Logger.terminate();
        CloseComms();
    }
}
