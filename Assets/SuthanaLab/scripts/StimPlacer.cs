using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StimPlacer : MonoBehaviour
{
    public List<GameObject> negativeStims;
    public List<float> negativeStimTimes;
    public GameObject StimTrigger;
    public List<Transform> routeBlueMarks;
    public List<Transform> routeRedMarks;
    public Transform FinalTrigger;
    public bool negativeOnly;
    public List<GameObject> singleBooks;
    public List<float> singleBookBaseAdjusts;
    public List<float> bookThicknesses;
    int bluemark_i, redmark_i;
    //counters
    int bats = 0, spiders = 0, snakes = 0, neutrals = 0;
    List<int> mtotals;
    List<int> totals;
    int maxreps=1;
    int creatureNum = 3;

    bool stimToLeft;
    int stim_i=0;
    int corridorCount = 0;
    List<GameObject> oldHidingBooks;

    public int stimNumber = 10;
    int redicecream = 12;
    int blueicecream = 4;
    
    Vector3 auxforth;
    Vector3 middlePoint;
    public Transform markIndicator;
    FearTaskManager fearTaskManager;
    public enum creatures {
        bats,
        spider,
        snake,
        vespa
    }

    // Start is called before the first frame update
    void Start()
    {
        maxreps = 2;
        creatureNum = creatures.GetNames(typeof(creatures)).Length;
        totals = new List<int>();
        mtotals = new List<int>();
        //for creatureNum creatures
        for (int i=0;i< creatureNum; i++) {
            totals.Add(0);
            mtotals.Add(maxreps);
        }

        //special icecream corridor
        redicecream = 12;
        blueicecream = 4;

        fearTaskManager = GetComponent<FearTaskManager>();

        if (stimNumber > 0)
        {
            fearTaskManager.fearStims.Clear();
        }
        oldHidingBooks = new List<GameObject>();
        //build the fears
        BuildFears();
        
        //activating the very first trigger
        fearTaskManager.fearStims[0].TriggerLocation.gameObject.SetActive(true);
        
        //Debugging
        //StartCoroutine(NextMarkSpotCoro());
    }

    void Update() {
        if(Input.GetKeyDown(KeyCode.K)) {
            string ss = "randstims: ";
            for (int j=0;j<(maxreps)*creatureNum*3;j++) {
                ss += RandomizeStim()+",";
            }
            Debug.Log(ss);
            string zz = "totals: ";
            for (int jj = 0; jj < totals.Count; jj++)
            {
                zz += totals[jj]+",";
            }
            Debug.Log(zz);
        }
    }

    IEnumerator NextMarkSpotCoro() {
        yield return new WaitForSeconds(0.5f);
        markIndicator.position = NextMarkSpot(Random.Range(0, 10) < 5);
        markIndicator.forward = auxforth.normalized; 
        StartCoroutine(NextMarkSpotCoro());
    }
    Vector3 NextMarkSpot(bool TotheLeft, creatures specials=0) {
        Vector3 nextSpot, nextSpotL,nextSpotR,auxforthL,auxforthR;
        float parcel = 0.2f;
        //avoiding shelf columns
        parcel = Random.Range(0.05f,0.15f) + 0.2f * Random.Range(0,4);
        if (specials == creatures.bats)//bats
            parcel = 0.1f;
        else if (specials == creatures.snake)//snake
            parcel = Random.Range(0.1f, 0.125f) + 0.2f * Random.Range(0, 4);
        else if (specials == creatures.vespa)
            parcel = 1.0f;

        //blue route
        if (bluemark_i < routeBlueMarks.Count - 3)
        {
            //Debug.Log("blue route " + bluemark_i);
            //special icecream corridor case
            if (bluemark_i == blueicecream){
                parcel = Random.Range(0.25f, 0.35f);
                if (specials == creatures.snake)
                    parcel = Random.Range(0.1f, 0.125f) + 0.2f;
                
                auxforth = routeBlueMarks[bluemark_i + 1].position - routeBlueMarks[bluemark_i].position;
                nextSpot = routeBlueMarks[bluemark_i].position + parcel * auxforth;
                middlePoint = 0.5f * (routeBlueMarks[1].position + routeBlueMarks[3].position) + 0.2f*auxforth;

                if (specials == creatures.vespa)
                {
                    nextSpot = middlePoint + 0.5f * auxforth;
                }

                bluemark_i += 2;
            } 
            else {
                    auxforthL = routeBlueMarks[bluemark_i + 1].position - routeBlueMarks[bluemark_i].position;
                    nextSpotL = routeBlueMarks[bluemark_i].position + parcel * auxforthL;
                    auxforthR = routeBlueMarks[bluemark_i + 3].position - routeBlueMarks[bluemark_i+2].position;
                    nextSpotR = routeBlueMarks[bluemark_i + 2].position + parcel * auxforthR;
                
                middlePoint = 0.5f * (nextSpotL + nextSpotR);
                if (TotheLeft)
                {
                    auxforth = auxforthL;
                    nextSpot = nextSpotL;
                }
                else
                {
                    nextSpot = nextSpotR;
                    auxforth = auxforthR;
                }
                if (specials == creatures.vespa)
                {
                    nextSpot = middlePoint;
                    middlePoint -= auxforth * (Random.Range(0.05f, 0.15f) + 0.2f * Random.Range(1, 3));
                }
                bluemark_i += 4;
            }
        }
        //red route
        else
        {
            if (redmark_i < routeRedMarks.Count - 3)
            {
                //Debug.Log("red route " + redmark_i);
                if (redmark_i == redicecream) {
                    parcel = Random.Range(0.05f, 0.15f) + 0.2f * Random.Range(1, 3);
                    if (specials == creatures.snake)
                        parcel = Random.Range(0.1f, 0.125f) + 0.2f;
                    
                    auxforth = routeRedMarks[redmark_i + 1].position - routeRedMarks[redmark_i].position;
                    nextSpot= routeRedMarks[redmark_i].position + parcel * auxforth;
                    middlePoint = 0.5f * (routeRedMarks[9].position + routeRedMarks[11].position)+0.2f*auxforth;
                    if (specials == creatures.vespa)
                    {
                        nextSpot = middlePoint + 0.5f * auxforth;
                    }
                    redmark_i += 2;
                }
                else {
                    auxforthL = routeRedMarks[redmark_i + 1].position - routeRedMarks[redmark_i].position;
                    nextSpotL= routeRedMarks[redmark_i].position + parcel * auxforthL;
                    auxforthR = routeRedMarks[redmark_i + 3].position - routeRedMarks[redmark_i+2].position;
                    nextSpotR = routeRedMarks[redmark_i + 2].position + parcel * auxforthR;
                    middlePoint = 0.5f * (nextSpotL + nextSpotR);
                    
                    if (TotheLeft)
                    {
                        auxforth = auxforthL;
                        nextSpot = nextSpotL;
                    }
                    else
                    {
                        nextSpot = nextSpotR;
                        auxforth = auxforthR;
                    }
                    if (specials == creatures.vespa)
                    {
                        nextSpot = middlePoint;
                        middlePoint -= auxforth * (Random.Range(0.05f, 0.15f) + 0.2f * Random.Range(1, 3));
                    }
                    redmark_i += 4;
                    if (redmark_i >= routeRedMarks.Count) {
                        redmark_i = 0;
                        bluemark_i = 0;
                    }
                }
            }
            else
            {
                //dead code
                bluemark_i = 0;
                redmark_i = 0;

                Debug.Log("restarted blue route " + bluemark_i);
                
                auxforthL = routeBlueMarks[bluemark_i + 1].position - routeBlueMarks[bluemark_i].position;
                nextSpotL = routeBlueMarks[bluemark_i].position + parcel * auxforthL;
                
                auxforthR = routeBlueMarks[bluemark_i + 3].position - routeBlueMarks[bluemark_i + 2].position;
                nextSpotR = routeBlueMarks[bluemark_i + 2].position + parcel * auxforthR;

                middlePoint = 0.5f * (nextSpotL + nextSpotR);
                if (TotheLeft)
                {
                    auxforth = auxforthL;
                    nextSpot = nextSpotL;
                }
                else
                {
                    nextSpot = nextSpotR;
                    auxforth = auxforthR;
                }
                bluemark_i += 4;
            }
        }
        return nextSpot;
    }
    public void BuildFears() {
        
        int stimChoice;
        creatures stimType = creatures.bats;
        corridorCount = 0;
        Vector3 stimpos = Vector3.zero;

        for (int i=0;i<oldHidingBooks.Count;i++) { Destroy(oldHidingBooks[i]); }
        oldHidingBooks.Clear();
        while(stim_i < stimNumber && corridorCount < 5)
        {
            stim_i++;
            corridorCount++;
            if (!negativeOnly)
            {
                int negatives = 0;
                for (int i=0;i<totals.Count;i++) { negatives += totals[i]; }
                if (neutrals < negatives - maxreps || neutrals == 0)
                {
                    //next has to be neutral
                    neutrals++;
                    stimChoice = -1;
                }
                else if (neutrals > negatives + maxreps)
                {
                    //next has to be negative
                    //stimChoice = RandomStim();
                    stimType = (creatures)RandomizeStim();
                    stimChoice = ProcessSpecialCases(stimType);
                }
                else
                {
                    //next can be anything
                    if (Random.Range(0, 10) < 5)
                    {
                        //neutral
                        neutrals++;
                        stimChoice = -1;
                    }
                    else
                    {
                        //negative
                        //stimChoice = RandomStim();
                        stimType = (creatures)RandomizeStim();
                        stimChoice = ProcessSpecialCases(stimType);
                    }
                }
            }
            else
            {
                //testing negative only
                //negative
                //stimChoice = RandomStim();
                stimType = (creatures)RandomizeStim();
                stimChoice = ProcessSpecialCases(stimType);
            }

            //forcing vespa
            //stimType = creatures.vespa;
            //stimChoice = ProcessSpecialCases(stimType);
            
            GameObject newStim = null;

            //adjustments
            if (stimChoice < 0)
            {
                //neutral
                stimpos = NextMarkSpot(false); 
            }
            //if spider
            else if (stimType == creatures.spider)
            {
                if (negativeStims.Count > 0)
                {
                    newStim = Instantiate(negativeStims[stimChoice]);
                    newStim.SetActive(false);
                    stimpos = NextMarkSpot(stimToLeft,stimType);
                    newStim.transform.position = stimpos;
                    //if going north                    
                    newStim.transform.forward = auxforth.normalized;
                    Vector3 posaux = newStim.transform.GetChild(0).transform.position;
                    posaux.y = 0f;
                    //putting the flee curve on the ground
                    newStim.transform.GetChild(0).transform.position = posaux;
                    //unparenting hiding books
                    oldHidingBooks.Add(newStim.transform.GetChild(newStim.transform.childCount - 1).gameObject);
                    newStim.transform.GetChild(newStim.transform.childCount-1).parent = null;
                    oldHidingBooks.Add(newStim.transform.GetChild(newStim.transform.childCount - 1).gameObject);
                    newStim.transform.GetChild(newStim.transform.childCount-1).parent = null;
                    oldHidingBooks.Add(newStim.transform.GetChild(newStim.transform.childCount - 1).gameObject);
                    newStim.transform.GetChild(newStim.transform.childCount-1).parent = null;
                }
                else { stimpos = NextMarkSpot(false); }
            }
            //if snake
            else if (stimType == creatures.snake){
                if (negativeStims.Count > 0)
                {
                    newStim = Instantiate(negativeStims[stimChoice]);
                    newStim.SetActive(false);
                    stimpos = NextMarkSpot(stimToLeft, stimType);
                    newStim.transform.position = stimpos;
                    //unparenting hiding books
                    oldHidingBooks.Add(newStim.transform.GetChild(newStim.transform.childCount - 1).gameObject);
                    newStim.transform.GetChild(newStim.transform.childCount-1).parent = null;
                    oldHidingBooks.Add(newStim.transform.GetChild(newStim.transform.childCount - 1).gameObject);
                    newStim.transform.GetChild(newStim.transform.childCount-1).parent = null;
                    oldHidingBooks.Add(newStim.transform.GetChild(newStim.transform.childCount - 1).gameObject);
                    newStim.transform.GetChild(newStim.transform.childCount-1).parent = null;
                }
                else { stimpos=NextMarkSpot(false); }
            }
            //if bat
            else if (stimType == creatures.bats) {
                if (negativeStims.Count > 0)
                {
                    newStim = Instantiate(negativeStims[stimChoice]);
                    newStim.SetActive(false);
                    stimpos = NextMarkSpot(stimToLeft, stimType);
                    middlePoint.y = 0f;
                    newStim.transform.position = middlePoint;
                }
                else { stimpos = NextMarkSpot(false); }
            }
            else if (stimType == creatures.vespa)
            {
                if (negativeStims.Count > 0)
                {
                    newStim = Instantiate(negativeStims[stimChoice]);
                    newStim.SetActive(false);
                    stimpos = NextMarkSpot(stimToLeft, stimType);
                    newStim.transform.position = stimpos;
                    newStim.transform.forward = auxforth.normalized;
                    if (stimToLeft) {
                        newStim.transform.GetChild(0).GetComponent<Vespa>().comeFromLeft = true;
                    }
                    else { newStim.transform.GetChild(0).GetComponent<Vespa>().comeFromLeft = false; }
                }
                else { stimpos = NextMarkSpot(false); }
            }

            //create the trigger
            GameObject newStimTrigger = Instantiate(StimTrigger);
            newStimTrigger.transform.position = middlePoint;
            newStimTrigger.transform.forward = auxforth.normalized;
            FearTaskManager.FearNode newStimNode = new FearTaskManager.FearNode();
            if (stimChoice >= 0)
            {
                newStimNode.fearStim = newStim;
                if (negativeStimTimes.Count > 0)
                    newStimNode.expositionTime = negativeStimTimes[stimChoice];
                else
                    newStimNode.expositionTime = 3f;
            }
            else
            {
                newStimNode.expositionTime = 3f;
            }
            newStimNode.TriggerLocation = newStimTrigger.transform;
            fearTaskManager.fearStims.Add(newStimNode);
            //deactivate the trigger so the FearTaskManager activates it when it is time
            newStimTrigger.SetActive(false);
            //Debug.Log("stim created:"+stimChoice+" blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);

            //populate shelf with books
            PopulateShelf(stimpos,(int)stimType);
        }
        if (stim_i >= stimNumber)
        {
            //move the final Trigger
            NextMarkSpot(false);
            FinalTrigger.position = middlePoint;
            FinalTrigger.forward = auxforth.normalized;
        }
        else {
            //deactivate the final trigger activated by mistake by the feartaskmanager
            FinalTrigger.gameObject.SetActive(false);
        }
        //activating next trigger
        fearTaskManager.fearStims[stim_i - 5].TriggerLocation.gameObject.SetActive(true);
        string ss = "created: ";
        for(int i=0;i<totals.Count;i++) { ss += (creatures)i + ":" + totals[i]+" "; }
        Debug.Log(ss);
        //Debug.Log("created: "+bats+" bats "+spiders+" spiders "+snakes+" snakes "+neutrals+" neutrals");
    }
    
    int RandomStim() {
        int randomStim = 0;
        if (bats < spiders - maxreps && bats < snakes - maxreps && bluemark_i != blueicecream && redmark_i != redicecream)
        {
            //bat
            bats++;
            //blue or red
            if (bluemark_i < routeBlueMarks.Count - 3) {
                //blue
                if (bluemark_i == 6 || bluemark_i == 14)
                {
                    //bats A
                    randomStim = 8;
                }
                else if (bluemark_i == 0 || bluemark_i == 10)
                {
                    //bats B
                    randomStim = 9;
                }
                else {
                    //Debug.Log("ERROR! There is no other option for bats in the RandomStim function!");
                    Debug.Log("ERROR! No BATS option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                }
            }
            else
            { //red
                if (redmark_i == 4 || redmark_i == 14)
                {
                    //bats A
                    randomStim = 8;
                }
                else if (redmark_i == 8)
                {
                    //bats B
                    randomStim = 9;
                }
                else if (redmark_i == 0)
                {
                    //bats C
                    randomStim = 10;
                }
                else { 
                    //Debug.Log("ERROR! There is no other option for bats in the RandomStim function!");
                    Debug.Log("ERROR! No BATS option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                }
            }
        }
        else if (spiders < bats - maxreps && spiders < snakes - maxreps)
        {
            //spider
            spiders++;
            if (bluemark_i < routeBlueMarks.Count - 3 && bluemark_i == blueicecream)
            { randomStim = 0; }
            else if (redmark_i == redicecream) { randomStim = 1; }
            else
            {
                randomStim = (Random.Range(0, 10) < 5 ? 0 : 1);
            }
            stimToLeft = (randomStim == 0);
        }
        else if (snakes < spiders - maxreps && snakes < bats - maxreps)
        {
            //snake
            snakes++;
            //blue or red
            if (bluemark_i < routeBlueMarks.Count - 3)
            {
                //blue
                if (bluemark_i == 6 || bluemark_i == 14)
                {
                    //going south
                    randomStim = Random.Range(0, 5) < 5 ? 2 : 3;
                    stimToLeft = randomStim == 2 ? true : false;
                }
                else if (bluemark_i == 0 || bluemark_i == 10)
                {
                    //going north
                    randomStim = Random.Range(0, 5) < 5 ? 4 : 5;
                    stimToLeft = randomStim == 4 ? true : false;
                }
                else if (bluemark_i == 4) {
                    //going right
                    randomStim = 7;
                    stimToLeft = true;
                }
                else
                {
                    //Debug.Log("ERROR! There is no other option for snake in the RandomStim function!");
                    Debug.Log("ERROR! No snake option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                }
            }
            else
            { //red
                if (redmark_i == 4 || redmark_i == 14)
                {
                    //going south
                    randomStim = Random.Range(0, 5) < 5 ? 2 : 3;
                    stimToLeft = randomStim == 2 ? true : false;
                }
                else if (redmark_i == 0 || redmark_i == 8)
                {
                    //going north
                    randomStim = Random.Range(0, 5) < 5 ? 4 : 5;
                    stimToLeft = randomStim == 4 ? true : false;
                }
                else if (redmark_i == 12)
                {
                    //going left
                    randomStim = 6;
                    stimToLeft = false;
                }
                else { 
                    //Debug.Log("ERROR! There is no other option for snake in the RandomStim function!");
                    Debug.Log("ERROR! No snake option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                }
            }
        }
        else
        {
            //any negative
            int anyrandom = Random.Range(1,3);
            if (anyrandom == 1 && bluemark_i != blueicecream && redmark_i != redicecream) {
                bats++;
                if (bluemark_i < routeBlueMarks.Count - 3)
                {
                    //blue
                    if (bluemark_i == 6 || bluemark_i == 14)
                    {
                        //bats A
                        randomStim = 8;
                    }
                    else if (bluemark_i == 0 || bluemark_i == 10)
                    {
                        //bats B
                        randomStim = 9;
                    }
                    else
                    {
                        //Debug.Log("ERROR! There is no other option for bats in the RandomStim function!");
                        Debug.Log("ERROR! No BATS option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                    }
                }
                else
                { //red
                    if (redmark_i == 4 || redmark_i == 14)
                    {
                        //bats A
                        randomStim = 8;
                    }
                    else if (redmark_i == 8)
                    {
                        //bats B
                        randomStim = 9;
                    }
                    else if (redmark_i == 0)
                    {
                        //bats C
                        randomStim = 10;
                    }
                    else { 
                        //Debug.Log("ERROR! There is no other option for bats in the RandomStim function!");
                        Debug.Log("ERROR! No BATS option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                    }
                }
            }
            else if (anyrandom == 2) {
                //snake
                snakes++;
                //blue or red
                if (bluemark_i < routeBlueMarks.Count - 3)
                {
                    //blue
                    if (bluemark_i == 6 || bluemark_i == 14)
                    {
                        //going south
                        randomStim = Random.Range(0, 5) < 5 ? 2 : 3;
                        stimToLeft = randomStim == 2 ? true : false;
                    }
                    else if (bluemark_i == 0 || bluemark_i == 10)
                    {
                        //going north
                        randomStim = Random.Range(0, 5) < 5 ? 4 : 5;
                        stimToLeft = randomStim == 4 ? true : false;
                    }
                    else if (bluemark_i == 4)
                    {
                        //going right
                        randomStim = 7;
                        stimToLeft = true;
                    }
                    else
                    {
                        Debug.Log("ERROR! No snake option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                    }
                }
                else
                { //red
                    if (redmark_i == 4 || redmark_i == 14)
                    {
                        //going south
                        randomStim = Random.Range(0, 5) < 5 ? 2 : 3;
                        stimToLeft = randomStim == 2 ? true : false;
                    }
                    else if (redmark_i == 0 || redmark_i == 8)
                    {
                        //going north
                        randomStim = Random.Range(0, 5) < 5 ? 4 : 5;
                        stimToLeft = randomStim == 4 ? true : false;
                    }
                    else if (redmark_i == 12)
                    {
                        //going left
                        randomStim = 6;
                        stimToLeft = false;
                    }
                    else { 
                        Debug.Log("ERROR! No snake option, blue_i:"+bluemark_i+" red_i:"+redmark_i+"  stims:"+fearTaskManager.fearStims.Count); 
                    }
                }
            }
            else {
                spiders++;
                if (bluemark_i < routeBlueMarks.Count - 3 && bluemark_i == blueicecream)
                { randomStim = 0; }
                else if (redmark_i == redicecream) { randomStim = 1; }
                else
                {
                    randomStim = (Random.Range(0, 10) < 5 ? 0 : 1);
                }                
                stimToLeft = (randomStim == 0);
            }
        }
        return randomStim;
    }
    int RandomizeStim()
    {
        int randomStim = 0;

        List<int> stimOptions = new List<int>();
        //for creatureNum creatures available choices are
        for (int i=0;i< creatureNum; i++) {
            stimOptions.Add(i);
        }
        //removing saturated choices
        for (int i=0;i<mtotals.Count;i++) {
            if (mtotals[i] <= 0)
            {
                stimOptions.Remove(i);
            }
        }
            //randomly sorting it for good measure, not great results though
            int rr = Random.Range(0, stimOptions.Count);
            stimOptions.Reverse(rr,stimOptions.Count-rr);
            /*
            string dd = "sorted options: ";        
            for (int i = 0; i < stimOptions.Count; i++)
            {
                dd += stimOptions[i] + ",";
            }
            Debug.Log(dd);
            */
        //randomly choosing 
        int randchoicei = Random.Range(0, stimOptions.Count);
        randomStim = stimOptions[randchoicei];
        //special case, no bats in the icecream corridor, forcing for the others
        if (randomStim == 0 && (bluemark_i == blueicecream || redmark_i == redicecream))
            randomStim = Random.Range(1,creatureNum);
        mtotals[randomStim]--;
        totals[randomStim]++;
        //Debug.Log("choice: "+ randomStim);
        //reset case
        bool stillValid = false;
        for (int i = 0; i < mtotals.Count; i++)
        {
            if (mtotals[i] > 0)
                stillValid = true;
        }
        if (!stillValid)
        {
            for (int i = 0; i < mtotals.Count; i++)
            {
                mtotals[i] = maxreps;
            }
        }

        return randomStim;
    }
    int ProcessSpecialCases(creatures creatureChoice) {
        int negativeOption = -1;
        switch (creatureChoice) {
            case creatures.bats:
                bats++;
                //blue or red
                if (bluemark_i < routeBlueMarks.Count - 3)
                {
                    //blue
                    if (bluemark_i == 6 || bluemark_i == 14)
                    {
                        //bats A
                        negativeOption = 8;
                    }
                    else if (bluemark_i == 0 || bluemark_i == 10)
                    {
                        //bats B
                        negativeOption = 9;
                    }
                    else
                    {
                        //Debug.Log("ERROR! There is no other option for bats in the RandomStim function!");
                        Debug.Log("ERROR! No BATS option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                    }
                }
                else
                { //red
                    if (redmark_i == 4 || redmark_i == 14)
                    {
                        //bats A
                        negativeOption = 8;
                    }
                    else if (redmark_i == 8)
                    {
                        //bats B
                        negativeOption = 9;
                    }
                    else if (redmark_i == 0)
                    {
                        //bats C
                        negativeOption = 10;
                    }
                    else
                    {
                        //Debug.Log("ERROR! There is no other option for bats in the RandomStim function!");
                        Debug.Log("ERROR! No BATS option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                    }
                }
                break;
            case creatures.spider:
                spiders++;
                if (bluemark_i < routeBlueMarks.Count - 3 && bluemark_i == blueicecream)
                { negativeOption = 0; }
                else if (redmark_i == redicecream) { negativeOption = 1; }
                else
                {
                    negativeOption = (Random.Range(0, 10) < 5 ? 0 : 1);
                }
                stimToLeft = (negativeOption == 0);
                break;
            case creatures.snake:
                snakes++;
                //blue or red
                if (bluemark_i < routeBlueMarks.Count - 3)
                {
                    //blue route
                    if (bluemark_i == 6 || bluemark_i == 14)
                    {
                        //going south
                        negativeOption = Random.Range(0, 5) < 5 ? 2 : 3;
                        stimToLeft = negativeOption == 2 ? true : false;
                    }
                    else if (bluemark_i == 0 || bluemark_i == 10)
                    {
                        //going north
                        negativeOption = Random.Range(0, 5) < 5 ? 4 : 5;
                        stimToLeft = negativeOption == 4 ? true : false;
                    }
                    else if (bluemark_i == 4)
                    {
                        //going right
                        negativeOption = 7;
                        stimToLeft = true;
                    }
                    else
                    {
                        //Debug.Log("ERROR! There is no other option for snake in the RandomStim function!");
                        Debug.Log("ERROR! No snake option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                    }
                }
                else
                { //red route
                    if (redmark_i == 4 || redmark_i == 14)
                    {
                        //going south
                        negativeOption = Random.Range(0, 5) < 5 ? 2 : 3;
                        stimToLeft = negativeOption == 2 ? true : false;
                    }
                    else if (redmark_i == 0 || redmark_i == 8)
                    {
                        //going north
                        negativeOption = Random.Range(0, 5) < 5 ? 4 : 5;
                        stimToLeft = negativeOption == 4 ? true : false;
                    }
                    else if (redmark_i == 12)
                    {
                        //going left
                        negativeOption = 6;
                        stimToLeft = false;
                    }
                    else
                    {
                        //Debug.Log("ERROR! There is no other option for snake in the RandomStim function!");
                        Debug.Log("ERROR! No snake option, blue_i:" + bluemark_i + " red_i:" + redmark_i + "  stims:" + fearTaskManager.fearStims.Count);
                    }
                }
                break;
            case creatures.vespa:
                negativeOption = 11;
                if (bluemark_i == blueicecream || bluemark_i == 0)
                    stimToLeft = false;
                else if (redmark_i == redicecream || (redmark_i == 0 && bluemark_i >= routeBlueMarks.Count))
                    stimToLeft = true;
                else
                    stimToLeft = Random.Range(0, 10) < 5;
                break;
        }
        return negativeOption;
    }
    void PopulateShelf(Vector3 Stimpos, int StimChoice) {
        float optimalDistance;

        switch (StimChoice) {
            case -1:
                optimalDistance = -1f;
                break;
            case 0://bats
                optimalDistance = -1f;
                break;
            case 1://spider
                optimalDistance = 0.125f;
                break;
            case 2://snake
                optimalDistance = 0.25f;
                break;
            default:
                optimalDistance = -1;
                Debug.Log("StimChoice option not available:"+StimChoice);
                break;
        }
        //treat special cases
        if (bluemark_i == blueicecream+2) {
            //only left
            CreateBooks(routeBlueMarks[bluemark_i-2].position, routeBlueMarks[bluemark_i+1-2].position, Stimpos, optimalDistance, true);
        }
        else if (redmark_i == redicecream+2) {
            Debug.Log("creating books for the redicecream");
            //only right
            CreateBooks(routeRedMarks[redmark_i-2].position, routeRedMarks[redmark_i-1].position, Stimpos, optimalDistance, false);
        }
        else {
            if (bluemark_i > 0 && redmark_i == 0){
                //blue route
                //left side
                CreateBooks(routeBlueMarks[bluemark_i-4].position, routeBlueMarks[bluemark_i+1-4].position, Stimpos, optimalDistance,true);
                //right side
                CreateBooks(routeBlueMarks[bluemark_i+2-4].position,routeBlueMarks[bluemark_i+3-4].position, Stimpos, optimalDistance,false);
            }
            else {//red route
                if (bluemark_i == 0) {
                    //last red corridor
                    //left side
                    CreateBooks(routeRedMarks[routeRedMarks.Count - 4].position, routeRedMarks[routeRedMarks.Count - 3].position, Stimpos, optimalDistance, true);
                    //right side
                    CreateBooks(routeRedMarks[routeRedMarks.Count - 2].position, routeRedMarks[routeRedMarks.Count - 1].position, Stimpos, optimalDistance, false);
                }
                else
                {
                    //left side
                    CreateBooks(routeRedMarks[redmark_i - 4].position, routeRedMarks[redmark_i + 1 - 4].position, Stimpos, optimalDistance, true);
                    //right side
                    CreateBooks(routeRedMarks[redmark_i + 2 - 4].position, routeRedMarks[redmark_i + 3 - 4].position, Stimpos, optimalDistance, false);
                }
            }
        }
    }
    void CreateBooks(Vector3 beginpos, Vector3 endpos, Vector3 Stimpos, float optimalDistance, bool leftside) {
        float parcel;
        Vector3 newpos;
        GameObject newbook;
        int booksPerSegment = 30;
        Vector3 forthdir = endpos-beginpos;
        int randomBook_i;
        int seg_i = 0;
        float segmentSize = 0.2f;
        float columnGap = 0.0125f;
        //float bookBaseAdjust = 0.1146f;
        if (singleBooks.Count>0) {
            //each shelf has 5 segments/columns
            while (seg_i < 5)
            {
                parcel = columnGap;// +Random.Range(0.0f, (segmentSize - 2 * columnGap) / (booksPerSegment));
                for (int i = 0; i <= booksPerSegment; i++)
                {
                    //parcel = columnGap + i* ((segmentSize-2*columnGap) / booksPerSegment);// +Random.Range(0.0f, (segmentSize-2*columnGap));
                    if (parcel > 0.1f)
                        randomBook_i = Random.Range(0, 5);
                    else
                        randomBook_i = Random.Range(0,singleBooks.Count);
                    newpos = beginpos + (parcel + seg_i * segmentSize) * forthdir;// + singleBookBaseAdjusts[randomBook_i] * Vector3.up;
                    if (Vector3.Distance(newpos, Stimpos) > optimalDistance * (stimToLeft==leftside?1:0) && parcel < segmentSize-2*columnGap)
                    {
                        newbook = GameObject.Instantiate(singleBooks[randomBook_i]);
                        newbook.transform.position = newpos+(leftside?Vector3.zero:bookThicknesses[randomBook_i]*forthdir.normalized);
                        newbook.transform.right = forthdir * (leftside?1:-1);
                        oldHidingBooks.Add(newbook);
                    }
                    //calculated
                    //parcel += ((segmentSize - 2 * columnGap) / booksPerSegment);
                    parcel += (bookThicknesses[randomBook_i]+ Random.Range(0.0f, 0.02f)) / forthdir.magnitude;
                }
                seg_i++;
            }
        }
    }
}
