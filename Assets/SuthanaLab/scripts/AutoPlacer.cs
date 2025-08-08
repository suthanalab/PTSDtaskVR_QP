using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoPlacer : MonoBehaviour
{
    public Transform libraryLocation;
    public Transform museumLocation;
    public Transform storeLocation;
    public Transform ratingsgrp;

    public string[] desiredStims;
    public int stimNum = 30;
    public int negativeStims = 15;
    public float neutralTime;

    public GameObject spiderPrefab;
    public float spiderSpawnHeight = 3f;
    public float exposingTime = 5.5f;
    public float desiredITI = 10f;
       

    public GameObject PlaceStim(Vector3 PlacePos) {
        PlacePos.y = spiderSpawnHeight;
        return Instantiate(spiderPrefab, PlacePos, Quaternion.identity);
    }
    public int DefineLocation(int locationID)
    {
        if (locationID < 1 && locationID > 3)
            locationID = 1;

        libraryLocation.gameObject.SetActive(false);
        museumLocation.gameObject.SetActive(false);
        storeLocation.gameObject.SetActive(false);
        switch (locationID)
        {
            case 1://library                
                //change the actual environment                
                libraryLocation.gameObject.SetActive(true);
                Logger.Loginfo(message: "Library is active");
                break;
            case 2://museum                
                museumLocation.gameObject.SetActive(true);
                Logger.Loginfo(message: "Museum is active");
                break;
            case 3://store                
                storeLocation.gameObject.SetActive(true);
                Logger.Loginfo(message: "Store is active");
                break;
        }
        return locationID;
    }
}
