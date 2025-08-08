using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class SphereRater : MonoBehaviour
{
    public Transform scaleBeginTransform;
    public Transform scaleEndTransform;
    Transform mtransform;
    
    public float rateValue;
    public Transform shadowRater;
    bool grabbingAvailable;
    bool grabbing;

    public bool ableyes;
    public bool ableno;
    public bool ableconfirm;

    public bool ltriggerLock;
    public bool rtriggerLock;
    public bool buttonLock;

    Transform grabbingHand;
    Transform originalParent;
    Transform ratingsTransform;
    Renderer[] uirenderers;
    Image[] uiimgs;
    Text[] uitexts;
        
    public float handleSpring = 10f;
    public Slider rateSlider;
    float rateScaleSize;
    MeshRenderer mmesh;
    public Text rateTxt;
    public string[] TaskQuestions = {
        "analog-Please rate your level of fear to the green light-0=Not scary/unpleasant\n5=Moderately scary\n10=Very scary/unpleasant",
        "digital-Did the green light lead to the surprising event?",
        "analog-How confident are you about your answer?-0=Not confident\n5=Moderately confident\n10=Very confident"};
    int question_i=-1;
    public Text questiontxt;
    public Text scaletxt;
    public GameObject analogPanel;
    public GameObject digitalPanel;
    Transform ratingGrp;
    Transform headTransform;
    public float lookdist = 1f;
    public float ratesensitivity = 0.5f;
    public FearTaskManager feartaskManager;
    public GameObject portalPrefab;
    public float relaxTime;
    public Transform beginSpot;
    bool invertFearColor;

    Collider mcollider;
   
    float ltriggervalue;
    float rtriggervalue;
    bool lprimar, rprimar, lsec, rsec;

    void Awake()
    {
        mtransform = transform;
        uirenderers = mtransform.parent.GetComponentsInChildren<Renderer>(true);
        uiimgs = mtransform.parent.GetComponentsInChildren<Image>(true);
        uitexts = mtransform.parent.GetComponentsInChildren<Text>(true);
        mmesh = GetComponent<MeshRenderer>();
        mcollider = GetComponent<Collider>();
    }

    // Start is called before the first frame update
    void Start()
    {
        ratingGrp = transform.parent.parent;                
        rateScaleSize = (scaleEndTransform.position - scaleBeginTransform.position).magnitude;        

        mmesh.enabled = false;
        headTransform = Camera.main.transform;
        ratingsTransform = transform.parent.parent;
        
        //NextQuestion();
    }

    // Update is called once per frame
    void Update()
    {
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.trigger, out ltriggervalue);
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.trigger, out rtriggervalue);

        if (Vector3.ProjectOnPlane(ratingGrp.position - headTransform.position, Vector3.up).magnitude > lookdist)
            ratingGrp.LookAt(Vector3.ProjectOnPlane(headTransform.position, Vector3.up));

        if ((rtriggervalue > 0.5 || ltriggervalue > 0.5) && grabbingAvailable && grabbingHand) {
            grabbing = true;
            originalParent = mtransform.parent;
            mtransform.parent = grabbingHand;
            mmesh.enabled = false;
        }

        if (grabbing)
        {
            
            if ((rtriggervalue < 0.5 && ltriggervalue < 0.5))
            {
                grabbing = false;
                mtransform.parent = originalParent;
                //mmesh.enabled = false;
            }
            //calculate the rate
            rateValue = Vector3.Dot(mtransform.position-scaleBeginTransform.position, scaleEndTransform.position - scaleBeginTransform.position)/(rateScaleSize*rateScaleSize);
        }
        else {
            //return handle
            mtransform.position = Vector3.Lerp(mtransform.position, shadowRater.position, handleSpring*Time.deltaTime);
        }
        //keyboard voting
        if (analogPanel.activeSelf && Input.GetKey(KeyCode.Period))
        {
            rateValue += ratesensitivity * Time.deltaTime;
        }
        if (analogPanel.activeSelf && Input.GetKey(KeyCode.Comma))
        {
            rateValue -= ratesensitivity * Time.deltaTime;
        }

        rateValue = Mathf.Clamp(rateValue, 0f, 1f);
        rateTxt.text = (rateValue * 10f).ToString("0.00");
        shadowRater.position = scaleBeginTransform.position + rateValue * (scaleEndTransform.position - scaleBeginTransform.position);
        rateSlider.value = rateValue;
        
        //voting
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.primaryButton, out lprimar);
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.primaryButton, out rprimar);
        InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).TryGetFeatureValue(CommonUsages.secondaryButton, out lsec);
        InputDevices.GetDeviceAtXRNode(XRNode.RightHand).TryGetFeatureValue(CommonUsages.secondaryButton, out rsec);

        //button voting
        if (digitalPanel.activeSelf)
        {
            if ((Input.GetKeyDown(KeyCode.Y) || lprimar || rprimar) && !buttonLock)
            {
                buttonLock = true;
                YesNoButton(true);
            }
            if ((Input.GetKeyDown(KeyCode.N) || lsec || rsec) && !buttonLock)
            {
                buttonLock = true;
                YesNoButton(false);
            }
        }
        else if (analogPanel.activeSelf)
        {
            if ((Input.GetKeyDown(KeyCode.Y) || lprimar || rprimar) && !buttonLock)
            {
                buttonLock = true;
                ConfirmRate();
            }
        }
        else{
            if ((Input.GetKeyDown(KeyCode.Y) || lprimar || rprimar) && !buttonLock)
            {
                buttonLock = true;
                YesNoButton(true);
            }
            if ((Input.GetKeyDown(KeyCode.N) || lsec || rsec) && !buttonLock)
            {
                buttonLock = true;
                YesNoButton(false);
            }
        }


        if(!lprimar && !rprimar && !lsec && !rsec) {
            buttonLock = false;
        }
        if(Input.GetKeyUp(KeyCode.Y) || Input.GetKeyUp(KeyCode.N))
        {
            buttonLock = false;
        }

        //trigger voting
        if ((rtriggervalue > 0.5f && !rtriggerLock )|| (ltriggervalue > 0.5f && !ltriggerLock))
        {
            //confirm
            if (ableconfirm) { ConfirmRate(); }
            else if (ableyes) { YesNoButton(true); }
            else if (ableno) { YesNoButton(false); }

            ltriggerLock = true;
            rtriggerLock = true;
        }
        if (rtriggervalue < 0.5f) { rtriggerLock = false; }
        if (ltriggervalue < 0.5f) { ltriggerLock = false; }
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "GameController")
        {
            grabbingAvailable = true;
            grabbingHand = other.transform;
            mmesh.enabled = true;
            //Debug.Log("Triggered by: " + other.name);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.tag == "GameController")
        {
            grabbingAvailable = false;
            grabbingHand = null;
            mmesh.enabled = false;
        }
    }
    void ConfirmRate() {
        if (question_i >= 0)
        {
            if (question_i != TaskQuestions.Length / 2) {
                if (question_i < TaskQuestions.Length / 2)
                {
                    //move to the next question if there is any
                    Debug.Log("Confirmed rate");
                    Logger.Loginfo(message: "After task rate for: " + question_i + ":" + TaskQuestions[question_i].Split('-')[2] + " was " + (rateValue * 10f).ToString("0.00"));
                }
                else{
                    Debug.Log("Confirmed rate");
                    Logger.Loginfo(message: "After task rate for: " + (question_i-1) + ":" + TaskQuestions[question_i-1].Split('-')[2] + " was " + (rateValue * 10f).ToString("0.00"));                    
                }
                NextQuestion();
            }
        }
        else {
            //start the Prepare4Questions
            StartCoroutine(Prepare4Questions());
        }
    }
    void YesNoButton(bool yes) {
        if (question_i>=0) {
            if(question_i != TaskQuestions.Length / 2) {
                if (question_i < TaskQuestions.Length / 2){
                    Logger.Loginfo(message: "After task answer for: " + question_i + ":" + TaskQuestions[question_i].Split('-')[2] + " was " + (yes ? "YES" : "NO"));
                }
                else { Logger.Loginfo(message: "After task answer for: " + (question_i-1) + ":" + TaskQuestions[question_i-1].Split('-')[2] + " was " + (yes ? "YES" : "NO")); 
                }
                NextQuestion();
            }
        }
        else { StartCoroutine(Prepare4Questions()); }
    }
    IEnumerator Prepare4Questions() {
        questiontxt.text = "";
        //change the light in the scene accordingly 
        feartaskManager.ChangeEnvLightColor(false, Fear: true);
        //play the sound
        feartaskManager.StimSound(NegativeSound: true);
        yield return new WaitForSeconds(feartaskManager.prepareTime);
        //change it back
        feartaskManager.ChangeEnvLightColor(false,Fear:false,Back2Normal:true);
        feartaskManager.StimSound(Stop: true);
        NextQuestion();
    }
    IEnumerator Prepare4NeutralQuestions()
    {
        questiontxt.text = "";
        analogPanel.SetActive(false);
        digitalPanel.SetActive(false);
        mcollider.enabled = false;
        shadowRater.gameObject.SetActive(false);

        //change the light in the scene accordingly 
        feartaskManager.ChangeEnvLightColor(false, Fear: false);
        //play the sound
        feartaskManager.StimSound(NegativeSound: false);
        yield return new WaitForSeconds(feartaskManager.prepareTime);
        //change it back
        feartaskManager.ChangeEnvLightColor(false, Fear: false, Back2Normal: true);
        feartaskManager.StimSound(Stop: true);
        NextQuestion();
    }
    void AdjustUIcolor(Color newguicolor) {
                
        for (int i = 0; i < uirenderers.Length; i++)
        {
            Material amat = uirenderers[i].material;
            amat.color = newguicolor;
            uirenderers[i].material=amat;
        }
        
        for (int i = 0; i < uiimgs.Length; i++)
        {
            float thisalpha = uiimgs[i].color.a;
            Color zz = newguicolor;
            zz.a = thisalpha;
            uiimgs[i].color = zz;

        }
        
        for (int i = 0; i < uitexts.Length; i++)
        {
            float thisalpha = uitexts[i].color.a;
            Color zz = newguicolor;
            zz.a = thisalpha;
            uitexts[i].color = zz;
        }
    }
    public void InvertFearColors(bool InvertFearColor) { invertFearColor = InvertFearColor; }
    public void NextQuestion() {
        grabbingAvailable = false;
        grabbingHand = null;
        mmesh.enabled = false;
        ltriggerLock = true;
        rtriggerLock = true;
        ableconfirm = false;
        ableno = false;
        ableyes = false;

        question_i++;
        if (question_i <= TaskQuestions.Length)
        {
            Color ratingcolor = Color.green;
            if(invertFearColor)
                ratingcolor = Color.blue;
            //if(TaskQuestions[question_i].Split('-')[0] == "blue")
            if (question_i >= TaskQuestions.Length / 2)
            {
                if (!invertFearColor)
                    ratingcolor = Color.blue;
                else
                    ratingcolor = Color.green;
            }

            ratingcolor.a = 0.4f;
            AdjustUIcolor(ratingcolor);

            if (question_i == TaskQuestions.Length / 2) {
                StartCoroutine(Prepare4NeutralQuestions());
            }
            else if(question_i > TaskQuestions.Length/2) {
                rateValue = 0f;
                if(!invertFearColor){
                    questiontxt.text = TaskQuestions[question_i - 1].Split('-')[2];

                    if (TaskQuestions[question_i - 1].Split('-')[1] == "digital")
                    {
                        analogPanel.SetActive(false);
                        digitalPanel.SetActive(true);
                        //gameObject.SetActive(false);
                        mcollider.enabled = false;
                        shadowRater.gameObject.SetActive(false);
                        scaletxt.gameObject.SetActive(false);
                    }
                    else
                    {
                        analogPanel.SetActive(true);
                        digitalPanel.SetActive(false);
                        //gameObject.SetActive(true);
                        mcollider.enabled = true;
                        shadowRater.gameObject.SetActive(true);
                        scaletxt.gameObject.SetActive(true);
                        scaletxt.text = TaskQuestions[question_i - 1].Split('-')[3];
                    }
                }
                else {
                    questiontxt.text = TaskQuestions[question_i - 1 - TaskQuestions.Length/2].Split('-')[2];

                    if (TaskQuestions[question_i - 1 - TaskQuestions.Length/2].Split('-')[1] == "digital")
                    {
                        analogPanel.SetActive(false);
                        digitalPanel.SetActive(true);
                        //gameObject.SetActive(false);
                        mcollider.enabled = false;
                        shadowRater.gameObject.SetActive(false);
                        scaletxt.gameObject.SetActive(false);
                    }
                    else
                    {
                        analogPanel.SetActive(true);
                        digitalPanel.SetActive(false);
                        //gameObject.SetActive(true);
                        mcollider.enabled = true;
                        shadowRater.gameObject.SetActive(true);
                        scaletxt.gameObject.SetActive(true);
                        scaletxt.text = TaskQuestions[question_i - 1-TaskQuestions.Length/2].Split('-')[3];
                    }
                }
            }
            else
            {
                if (!invertFearColor)
                {
                    rateValue = 0f;
                    questiontxt.text = TaskQuestions[question_i].Split('-')[2];

                    if (TaskQuestions[question_i].Split('-')[1] == "digital")
                    {
                        analogPanel.SetActive(false);
                        digitalPanel.SetActive(true);
                        //gameObject.SetActive(false);
                        mcollider.enabled = false;
                        shadowRater.gameObject.SetActive(false);
                        scaletxt.gameObject.SetActive(false);
                    }
                    else
                    {
                        analogPanel.SetActive(true);
                        digitalPanel.SetActive(false);
                        //gameObject.SetActive(true);
                        mcollider.enabled = true;
                        shadowRater.gameObject.SetActive(true);
                        scaletxt.gameObject.SetActive(true);
                        scaletxt.text = TaskQuestions[question_i].Split('-')[3];
                    }
                }
                else {
                    rateValue = 0f;
                    questiontxt.text = TaskQuestions[question_i+TaskQuestions.Length/2].Split('-')[2];

                    if (TaskQuestions[question_i+TaskQuestions.Length/2].Split('-')[1] == "digital")
                    {
                        analogPanel.SetActive(false);
                        digitalPanel.SetActive(true);
                        //gameObject.SetActive(false);
                        mcollider.enabled = false;
                        shadowRater.gameObject.SetActive(false);
                        scaletxt.gameObject.SetActive(false);
                    }
                    else
                    {
                        analogPanel.SetActive(true);
                        digitalPanel.SetActive(false);
                        //gameObject.SetActive(true);
                        mcollider.enabled = true;
                        shadowRater.gameObject.SetActive(true);
                        scaletxt.gameObject.SetActive(true);
                        scaletxt.text = TaskQuestions[question_i+TaskQuestions.Length/2].Split('-')[3];
                    }
                }
            }
        }
        else {
            //reset it for extinction
            question_i = -1;
            
            Finish();
        }
    }
    public void Finish() {
        //questiontxt.text = "Habituation round";
        questiontxt.text = "";
        StartCoroutine(ClearUI());
        analogPanel.SetActive(false);
        digitalPanel.SetActive(false);
        //gameObject.SetActive(false);
        mcollider.enabled = false;
        shadowRater.gameObject.SetActive(false);

        //close logfile and comms
        //do not close the log to wait for the last habituation round
        //feartaskManager.CloseComms();
        feartaskManager.ResetTask();
    }
    IEnumerator ClearUI() {
        yield return new WaitForSeconds(5.0f);
        questiontxt.text = "";
    }

    public void ResetNeutral() {
        questiontxt.text = "";
    }
}
