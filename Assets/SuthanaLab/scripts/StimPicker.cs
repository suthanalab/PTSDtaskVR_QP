using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StimPicker : MonoBehaviour
{
    //this list is multiple of 10, the 8 first correspond to the normal corridors and the 2 last are the special cases
    public List<Transform> fears;
    public List<Transform> triggers;
    Transform fearsContainer;
    
    public List<float> negativeTimes;
    public GameObject StimTrigger;
    public bool negativeOnly;

    public Transform indicator;
    public Transform finalTrigger;
    public Transform ratingsgrp;
    public int stimNum = 30;
    public int negativeStims = 15;
    
    int stimsPerCorridor;
    int jj=0;    
    
    //use -1 for specifying special corridors position to indicate there is no such special case
    int posx = 2;
    int posy = 8;

    public float neutralTime = 3f;
    [Tooltip("-1 for random\n0 for bats\n1 for spider\n2 for cobra\n3 for wasp\n4 for rats")]
    public int specificCreature = -1;
    
    public bool notrandomize;
    
    public Transform libStims;
    public Transform museumStims;
    public Transform storeStims;
    public Transform libraryLocation;
    public Transform museumLocation;
    public Transform storeLocation;

    List<GameObject> neutralTriggers;

    bool alreadyStarted;
    public bool skipCorridor5;
    public string[] desiredStims;
    List<int> usedstims;
    int repspider = 0, repvespa = 0;

    public Transform turnRefA;
    public Transform turnRefB;

    enum creatures
    {
        bat, spider, cobra, vespa, rat
    };

    void Awake() {
        neutralTriggers = new List<GameObject>();
    }

    public void AlreadyStarted() {
        alreadyStarted = true;
    }

    bool VerifyDesiredFears(int CorridorsNum) {
        int[] ffs = new int[CorridorsNum];
        int corridor = 0;
        bool notpossible = false;
        int lap = 1;
        for(int i=0;i<desiredStims.Length;i++){
            if(desiredStims[i]!="n"){
                ffs[corridor]+=1;
                if(ffs[corridor] > stimsPerCorridor) {
                    notpossible=true;
                    break;
                }
            }
            corridor++;
            if(corridor >= CorridorsNum)
            {
                corridor = 0;
                lap++;
            }
        }

        if (notpossible)
        {
            Debug.Log("<color=red>Desired stims list is impossible because there are only "+stimsPerCorridor+" negative stims per corridor. On corridor " + corridor + " round "+lap+" there were no more negative stims to draw from.</color>");
            return false;
        }
        else {
            return true;
        }
    }
    void SortVespasNSpiders(int CorridorsNum) {
        int col = 0;
        int j = 0;
        string dd = " Longest seq possible=";
        int maxstreak = 0;
        int jj = 0;
        
        Transform tt;

        repspider = 0;
        repvespa = 0;
        int randtol = 1;

        for (int i = 0; i < fears.Count; i++)
        {
            jj = j + col * stimsPerCorridor;

            int r = 0; //index of the item to swap
            if (fears[jj].name.Contains("spider") || fears[jj].name.Contains("vespa"))
            {
                if (fears[jj].name.Contains("vespa"))
                {
                    //check if there are spiders left
                    bool nospiders = true;
                    for (int gg = 1 + j + col * stimsPerCorridor; gg < (col + 1) * stimsPerCorridor; gg++)
                    {
                        if (fears[gg].name.Contains("spider"))
                        {
                            r = gg;
                            nospiders = false;
                            break;
                        }
                    }
                    if (!nospiders)
                    {
                        //check if you need to make the swap
                        if (repvespa > randtol || repvespa - repspider > 1 + randtol || Random.Range(0, 10) > 5)
                        {
                            //swap in case there are spiders left
                            //swap
                            tt = fears[r];
                            fears[r] = fears[jj];
                            fears[jj] = tt;
                            tt = triggers[r];
                            triggers[r] = triggers[jj];
                            triggers[jj] = tt;
                            repspider++;
                            repvespa = 0;
                        }
                        else
                        {
                            repvespa++;
                            repspider = 0;
                        }
                    }
                    else { repvespa++; repspider = 0; }
                }
                else
                {
                    //check if there are vespas left
                    bool novespas = true;
                    for (int gg = 1 + j + col * stimsPerCorridor; gg < (col + 1) * stimsPerCorridor; gg++)
                    {
                        if (fears[gg].name.Contains("vespa"))
                        {
                            r = gg;
                            novespas = false;
                            break;
                        }
                    }
                    if (!novespas)
                    {
                        //check if you need to make the swap
                        if (repspider > randtol || repspider - repvespa > randtol + 1 || Random.Range(0, 10) > 5)
                        {
                            //swap in case there are spiders left
                            //swap
                            tt = fears[r];
                            fears[r] = fears[jj];
                            fears[jj] = tt;
                            tt = triggers[r];
                            triggers[r] = triggers[jj];
                            triggers[jj] = tt;
                            repvespa++;
                            repspider = 0;
                        }
                        else
                        {
                            repspider++;
                            repvespa = 0;
                        }
                    }
                    else { repspider++; repvespa = 0; }
                }
            }

            if (fears[jj].name.Contains("spider") || fears[jj].name.Contains("vespa"))
            {
                dd += fears[jj].name + ",";
                maxstreak++;
            }

            col++;
            if (col >= CorridorsNum)
            {
                col = 0;
                j++;
            }
        }
        Debug.Log("<color=cyan>Max streak:"+maxstreak+dd+"</color>");
    }
    public bool PickStims(FearTaskManager taskManager, bool extinction=false, float mdist=7.0f) {

        int corridorsNum = (8-(skipCorridor5?2:0) + (posx > 0 ? 1 : 0) + (posy > 0 ? 1 : 0));
        stimsPerCorridor = fears.Count / corridorsNum;
        Debug.Log("stimspercorridor: " + stimsPerCorridor + ", corridors: " + corridorsNum);
        if (negativeStims > fears.Count && !extinction)
        {
            int neutralstims = stimNum - negativeStims;
            negativeStims = fears.Count;
            stimNum = neutralstims + negativeStims;
            Debug.Log("<color=yellow>stimnum was changed because there were not enough fears</color>");
        }

        //verify desired fears
        if (!VerifyDesiredFears(corridorsNum)) {
            Time.timeScale = 0.0f;
            Application.Quit();
            return false;
        }

        //walk through the list as a player and evenly distribute vespas and spiders
        //SortVespasNSpiders(corridorsNum);

        int stims = 0;
        int qq = 1;
        int ii = 1;
        
        stims = 0;
        int specialcasedetected = 0;
        repspider = 0;
        repvespa = 0;

        int creature = 0;
        for (int i = 0; i < fears.Count; i++)
        {
            if (fears[i].name.Contains("bat"))
                creature = 0;
            else if (fears[i].name.Contains("spider"))
                creature = 1;
            else if (fears[i].name.Contains("cobra"))
                creature = 2;
            else if (fears[i].name.Contains("vespa"))
                creature = 3;
            else if (fears[i].name.Contains("rat"))
                creature = 4;

            //to be safe, make everything not active
            fears[i].gameObject.SetActive(false);
            triggers[i].gameObject.SetActive(false);
            //create the new fearnode
            FearTaskManager.FearNode thisnode = new FearTaskManager.FearNode();
            thisnode.fearStim = fears[i].gameObject;
            thisnode.TriggerLocation = triggers[i];
            thisnode.expositionTime = negativeTimes[creature];

            //populating queues
            stims++;
            if (stims > stimsPerCorridor)
            {
                stims = 1;
                qq++;
                if (qq > corridorsNum)
                    qq = 1;
            }
            //Debug.Log(fears[i].name + " on corridor queue:"+qq);
        }

        if (specialcasedetected > 0)
        {
            if (posx > 0 && specialcasedetected == 1)
            {
                Debug.Log("One special corridor detected and taken into account");
            }
            else if (posx > 0 && specialcasedetected == 2 && posy > 0)
            {
                Debug.Log("Two special corridor detected and taken into account");
            }
            else { Debug.Log("Some weird special corridor case detected, but posx or posy seem wrong, or the amount of fears listed is off, please fix"); }
        }
        else
        {
            if (posx > 0 || posy > 0)
            {
                Debug.Log("No special corridor case detected in the fears list despite the position indicators asking for them");
                if (posx > 0)
                    Debug.Log("Either add the special cases to the fears list or set posx to -1.");
                if (posy > 0)
                    Debug.Log("Either add the special cases to the fears list or set posy to -1.");
            }
            else { Debug.Log("No special corridor cases detected, working with only 5 corridors"); }
        }

        //queues are populated, now walk the corridors

        //clear old manual fears
        taskManager.fearStims.Clear();

        //reset neutral triggers
        if (neutralTriggers.Count > 0)
        {
            for (int i = 0; i < neutralTriggers.Count; i++) { Destroy(neutralTriggers[i]); }
            neutralTriggers.Clear();
        }
        neutralTriggers = new List<GameObject>();


        ii = 0;
        int dd = 0; //desired index
        qq = 1; //corridor indicator
        int neutrals = 0;
        usedstims = new List<int>();
        float maxdist = -1;
        float mindist = 10000;
        Vector3 oldpos=Vector3.zero;
        int worst_i=0, best_i=0;
        //float mdist = 7.5f; //the smallest distance acceptable
        float dist=-1f;
        bool corridorskipped = false;
        bool skippedEdgeCorridors=false;

        while (dd < stimNum)
        {
            if (desiredStims[dd] == "n")
            {
                int nn = (qq - 1) * stimsPerCorridor + Random.Range(0, stimsPerCorridor);

                int bb;
                if (corridorskipped) {
                    bb = nn;
                    corridorskipped = false;
                    dist = -1f;
                }
                else {
                    dist = CalculateStimsDist(qq, triggers[nn].position, oldpos);
                    bb = TryGetBetterDistanceBetweenStims(ii, dist, mdist, nn, qq, oldpos, stimnorepeat: false);
                }

                if (bb >= 0)
                {
                    if (bb != nn)
                    {
                        nn = bb;
                        dist = CalculateStimsDist(qq, triggers[nn].position, oldpos);
                    }
                    GameObject newStimTrigger = Instantiate(StimTrigger);
                    newStimTrigger.transform.position = triggers[nn].position;
                    newStimTrigger.transform.forward = triggers[nn].right;
                    newStimTrigger.gameObject.SetActive(false);
                    neutralTriggers.Add(newStimTrigger);
                    //create the new fearnode
                    FearTaskManager.FearNode thisnode = new FearTaskManager.FearNode();
                    thisnode.fearStim = null;
                    thisnode.TriggerLocation = newStimTrigger.transform;
                    thisnode.expositionTime = neutralTime;
                    taskManager.fearStims.Add(thisnode);
                    neutrals++;
                    oldpos = triggers[nn].position;
                    dd++;
                    Debug.Log("<color=yellow>Satisfied mdist with dist:" + dist.ToString("0.00") + "</color>");
                }
                else {
                    //next time there is no need to check dist
                    corridorskipped = true;
                    //it could not satisfy the mindist, skip corridor
                    Debug.Log("<color=orange>Could not satisfy mindist, skipping corridor " + qq + "</color>");
                    if (qq == 4 || qq == 1)
                        skippedEdgeCorridors = true;
                }
            }
            else
            {
                neutrals = 0;
                if (extinction) {
                    int nn = (qq - 1) * stimsPerCorridor + Random.Range(0, stimsPerCorridor);

                    int bb;
                    if(corridorskipped) {
                        bb = nn;
                        corridorskipped = false;
                        dist = -1f;
                    }
                    else {
                        dist = CalculateStimsDist(qq, triggers[nn].position, oldpos);
                        bb = TryGetBetterDistanceBetweenStims(ii, dist, mdist, nn, qq, oldpos, stimnorepeat: false);
                    }
                    if (bb >= 0)
                    {
                        if (bb != nn)
                        {
                            nn = bb;
                            dist = CalculateStimsDist(qq, triggers[nn].position, oldpos);
                        }
                        GameObject newStimTrigger = Instantiate(StimTrigger);
                        newStimTrigger.transform.position = triggers[nn].position;
                        newStimTrigger.transform.forward = triggers[nn].right;
                        newStimTrigger.gameObject.SetActive(false);
                        neutralTriggers.Add(newStimTrigger);
                        //create the new fearnode
                        FearTaskManager.FearNode thisnode = new FearTaskManager.FearNode();
                        //create an empty stim
                        GameObject gg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        gg.GetComponent<Renderer>().enabled = false;
                        gg.GetComponent<Collider>().enabled = false;

                        thisnode.fearStim = gg;
                        thisnode.TriggerLocation = newStimTrigger.transform;
                        thisnode.expositionTime = neutralTime;
                        taskManager.fearStims.Add(thisnode);
                        oldpos = triggers[nn].position;
                        dd++;
                        Debug.Log("<color=yellow>Satisfied mdist with dist:" + dist.ToString("0.00") + "</color>");
                    }
                    else {
                        //next time there is no need to check dist
                        corridorskipped = true;
                        //it could not satisfy the mindist, skip corridor
                        Debug.Log("<color=orange>Could not satisfy mindist, skipping corridor " + qq+"</color>");
                        
                        if (qq == 4 || qq == 1)
                            skippedEdgeCorridors = true;
                    }
                }
                else
                {
                    int rr = 0;
                    //desiredStims[ii]
                    //rr = Random.Range((qq - 1) * stimsPerCorridor, qq * stimsPerCorridor);
                    rr = (qq - 1) * stimsPerCorridor + Random.Range(0, stimsPerCorridor);

                    int randcounter=1;
                    //Debug.Log("entered loop for random stim");
                    while(usedstims.Contains(rr) && randcounter < 20) {
                        //rr = Random.Range((qq - 1) * stimsPerCorridor, qq * stimsPerCorridor);
                        rr = (qq - 1) * stimsPerCorridor + Random.Range(0, stimsPerCorridor);
                        randcounter++;
                    }
                    //Debug.Log("number of loop for random stim attempts:"+randcounter);
                    //investigate if there is nothing left indeed
                    if(randcounter >= 20){
                        for (int i=0;i<stimsPerCorridor;i++){
                            int kk = (qq - 1) * stimsPerCorridor + i;
                            if (!usedstims.Contains(kk))
                            {
                                rr = kk;
                                Debug.Log("<color=yellow>able to find an unused stim despite random fail:"+kk+"</color");
                            }
                        }
                    }

                    int bb;

                    if(corridorskipped){
                        bb = rr;
                        corridorskipped = false;
                        dist = -1f;
                    }
                    else {
                        //Debug.Log("<color=orange>needed:" + randcounter + " rand calls</color>");
                        dist = CalculateStimsDist(qq, triggers[rr].position, oldpos);
                        bb = TryGetBetterDistanceBetweenStims(ii, dist, mdist, rr, qq, oldpos, stimnorepeat: true);
                    }
                    if (bb >= 0)
                    {
                        if (bb != rr)
                        {
                            rr = bb;
                            dist = CalculateStimsDist(qq, triggers[rr].position, oldpos);
                        }
                        usedstims.Add(rr);

                        if (fears[rr].name.Contains("bat"))
                        {
                            creature = 0;
                        }
                        else if (fears[rr].name.Contains("spider"))
                        {
                            creature = 1;
                            repspider++;
                            repvespa = 0;
                        }
                        else if (fears[rr].name.Contains("cobra"))
                        {
                            creature = 2;
                        }
                        else if (fears[rr].name.Contains("vespa"))
                        {
                            creature = 3;
                            repvespa++;
                            repspider = 0;
                        }
                        else if (fears[rr].name.Contains("rat"))
                        {
                            creature = 4;
                        }

                        FearTaskManager.FearNode thisnode = new FearTaskManager.FearNode();
                        thisnode.fearStim = fears[rr].gameObject;
                        thisnode.TriggerLocation = triggers[rr];
                        thisnode.expositionTime = negativeTimes[creature];

                        taskManager.fearStims.Add(thisnode);
                        oldpos = triggers[rr].position;
                        dd++;
                        Debug.Log("<color=yellow>Satisfied mdist with dist:" + dist.ToString("0.00") + "</color>");
                    }
                    else
                    {
                        //next time there is no need to check dist
                        corridorskipped = true;
                        //it could not satisfy the mindist, skip corridor
                        Debug.Log("<color=orange>Could not satisfy mindist, skipping corridor " + qq + "</color>");

                        if (qq == 4 || qq == 1)
                            skippedEdgeCorridors = true;
                    }
                }
            }

            if(ii>0) {
                if (dist < mindist && dist >= 0f)
                {
                    mindist = dist;
                    worst_i = ii;
                }
                if (dist > maxdist)
                {
                    maxdist = dist;
                    best_i = ii;
                }
            }

            qq++;
            if (qq > corridorsNum - (posx > 0 ? 1 : 0) - (posy > 0 ? 1 : 0))
                qq = 1;            
            
            ii++;
        }
        Debug.Log("last corridor was: " + qq + " ii:" + ii);

        Debug.Log("<color=cyan>mindist_"+worst_i+"_:" + mindist +" maxdist_"+best_i+":"+maxdist+ "</color>");

        //reposition final trigger according to next corridor

        int randomnextcorridorpos = Random.Range((qq - 1) * stimsPerCorridor, qq * stimsPerCorridor);
        //randomnextcorridorpos = (qq - 1) * stimsPerCorridor;
        finalTrigger.position = triggers[randomnextcorridorpos].position;
        Vector3 ratingpos = finalTrigger.position;
        ratingpos.y = 0f;
        ratingsgrp.position = ratingpos;

        return skippedEdgeCorridors;
    }
    float CalculateStimsDist(int Atcorridor, Vector3 Currpos, Vector3 Oldpos) {        
        float distance;
        //turning corridor x coords: 3.46f and -5.49f
        if (Atcorridor % 2 == 0)
        {
            //1-2,3-4,5-6
            distance = Mathf.Abs(turnRefA.position.x - Currpos.x) + Mathf.Abs(turnRefA.position.x - Oldpos.x);
            //Debug.Log("<color=yellow>qq:" + qq + "</color>");
        }
        else
        {
            //2-3,4-5,6-1
            distance = Mathf.Abs(turnRefB.position.x - Currpos.x) + Mathf.Abs(turnRefB.position.x - Oldpos.x);
        }
            
        return distance;
    }

    int TryGetBetterDistanceBetweenStims(int node_i, float idist, float acceptabledist,int istim_i, int whichcorridor, Vector3 Oldpos, bool stimnorepeat=false) {
        if (node_i > 0)
        {
            //if random choice was no good, pick a good option   
            int goodoption = istim_i;
            if (idist < acceptabledist)
            {
                goodoption = -1;
            }

            List<int> goodoptions = new List<int>();

            int stimoption = (whichcorridor - 1) * stimsPerCorridor;
            for (int z = 0; z < stimsPerCorridor; z++)
            {
                float adist = CalculateStimsDist(whichcorridor, triggers[stimoption].position, Oldpos);
                if (adist > acceptabledist)
                {
                    if (stimnorepeat)
                    {
                        if (!usedstims.Contains(stimoption))
                            goodoptions.Add(stimoption);
                    }
                    else { goodoptions.Add(stimoption); }
                }

                stimoption++;
            }

            if (goodoptions.Count > 0)
            {
                //check repetition
                if (stimnorepeat) {
                    if (repspider > 2) {
                        goodoption = goodoptions[goodoptions.Count-1];
                    }
                    else if (repvespa > 2) {
                        goodoption = goodoptions[0];
                    }
                    else
                    {
                        goodoption = goodoptions[Random.Range(0, goodoptions.Count)];
                    }
                }
                else
                {
                    goodoption = goodoptions[Random.Range(0, goodoptions.Count)];
                }
            }

            if (goodoption < 0)
                Debug.Log("<color=red>No good options left.</color>");
            
            
            //Debug.Log("<color=yellow>dist:" + dist + "</color>");                        
            return goodoption;
        }
        else { return istim_i; }
    }

    public int DefineLocation(int locationID) {
        if (locationID < 1 && locationID > 3)
            locationID = 1;

        libraryLocation.gameObject.SetActive(false);
        museumLocation.gameObject.SetActive(false);
        storeLocation.gameObject.SetActive(false);
        switch (locationID) {
            case 1://library
                //change the parent transform for all the stims
                fearsContainer = libStims;
                //change the actual environment                
                libraryLocation.gameObject.SetActive(true);
                Logger.Loginfo(message: "Library is active");

                //no longer using special ice cream corridor case as it is in between 2 corridors and hard to balance
                posx = -1;
                posy = -1;
                /*
                //special corridor
                if (skipCorridor5) {
                    posx = 2;
                    posy = 6;
                }
                else
                {
                    posx = 2;
                    posy = 8;
                }
                */
                break;
            case 2://museum
                fearsContainer = museumStims;
                museumLocation.gameObject.SetActive(true);
                Logger.Loginfo(message: "Museum is active");
                posx = -1;
                posy = -1;
                break;
            case 3://store
                fearsContainer = storeStims;
                storeLocation.gameObject.SetActive(true);
                Logger.Loginfo(message: "Store is active");
                posx = -1;
                posy = -1;
                break;
        }
        PopulateFearsNTriggers();
        return locationID;
    }
    void PopulateFearsNTriggers()
    {
        //populating fears and triggers
        Transform localpar;
        fears = new List<Transform>();
        triggers = new List<Transform>();

        Transform potentialTrigger=null;

        for (int i = 0; i < fearsContainer.childCount; i++)
        {
            if(!(skipCorridor5 && (i==4 || i==5 || i>=8))) {
                localpar = fearsContainer.GetChild(i);
                for (int ss = 0; ss < localpar.childCount; ss++)
                {
                    if (localpar.GetChild(ss).name.Contains("stimTrigger"))
                    {
                        potentialTrigger = localpar.GetChild(ss);
                    }
                    else
                    {
                        //now we only consider the 2 insects currently in use
                        if(localpar.GetChild(ss).name.Contains("vespa") || localpar.GetChild(ss).name.Contains("spider"))
                        {
                            fears.Add(localpar.GetChild(ss));
                            triggers.Add(potentialTrigger);
                        }
                    }
                }
            }
        }
    }
}
