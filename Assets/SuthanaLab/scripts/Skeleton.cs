using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

using UnityEngine;

// bone
public class RigidBody
{
	public string name = "";
	public int ID = -1;
	public int parentID = -1;
	public float length = 1.0f;
	public Vector3 pos;
	public Quaternion ori;
	public Quaternion initOri;
	public Transform trans;
}

// skeleton
public class Skeleton
{
	public string name = "";
	public int ID = -1;
	public int nBones = 0;
	public RigidBody[] bones = new RigidBody[200];
	public bool bHasHierarchyDescription = false;
	public bool bNeedBoneLengths = true;
	public float fUnitConversion = .001f;   // arena mm to m
	public float refLength;

	public Dictionary<int, GameObject> RBToGameObject = new Dictionary<int, GameObject>();
	public Dictionary<GameObject, int> GameObjectToRB = new Dictionary<GameObject, int>();

	public GameObject rootObject = null;

	public Skeleton()
	{
		for (int i = 0; i < 200; i++)
		{
			bones[i] = new RigidBody();
		}
	}

	public virtual void UpdateBonePosOriFromFile(int frameIndex)
	{
	}

	// Create a skeleton from an existing unity rig
	public void CreateFromTransform(Transform t)
	{
		name = t.name;
		AddBones(t, -1);
		bNeedBoneLengths = false;
		bHasHierarchyDescription = true;
		Debug.Log("[Skeleton] Created skeleton from existing transforms (name:" + name + "  Bones:" + nBones + ")");
	}

	private void AddBones(Transform t, int parentIndex)
	{
		int index = nBones;
		bones[index].name = t.name;
		bones[index].ID = index;
		bones[index].parentID = parentIndex;
		bones[index].pos = t.localPosition;
		bones[index].ori = t.localRotation;
		bones[index].initOri = t.localRotation; // initial ori required to get character skeleton into init-pose (aka t-pose, neutral pose)
		bones[index].trans = t;
		if (t.childCount == 1)
		{
			bones[index].length = t.GetChild(0).localPosition.magnitude;

			// use upper leg as a "reference length" for this skeleton
			if (bones[index].name.Contains("Thigh") || bones[index].name.Contains("UpLeg"))
			{
				refLength = bones[index].length;
				Debug.Log(refLength);
			}

		}
		nBones++;
		//Debug.Log("[Skeleton] Created Bone: " + bones[index].name + "(length: "+ bones[index].length+")");

		RBToGameObject[bones[index].ID] = t.gameObject;
		GameObjectToRB[t.gameObject] = bones[index].ID;

		foreach (Transform tChild in t)
		{
			AddBones(tChild, index);
		}
	}

	public RigidBody GetBone(string boneName)
	{
		for (int i = 0; i < nBones; i++)
		{
			if (bones[i].name == boneName)
				return bones[i];
		}
		return null;
	}

	public RigidBody GetBone(int id)
	{
		for (int i = 0; i < nBones; i++)
		{
			if (bones[i].ID == id)
				return bones[i];
		}
		return null;
	}

	public void UpdateBoneLengths()
	{
		if (bHasHierarchyDescription)
		{
			for (int i = 0; i < nBones; i++)
			{
				RigidBody rb = bones[i];
				if (rb.parentID >= 0)
				{
					RigidBody rbParent = GetBone(rb.parentID);
					if (rbParent != null)
					{
						// todo : where an rb has multiple children, traditional bone length is undefined
						// for purpose of retargeting, could define it as average of all parent-child lengths, or min or max.

						Vector3 v = rb.pos;     // pos is already a local xform - no need to subtract out
						float fLength = v.magnitude;
						rbParent.length = fLength * fUnitConversion;
						//Debug.Log("[Skeleton] Updated Bone Length (Bone:" + rbParent.name + "  ID:" + rbParent.ID + "  Length:"+rbParent.length+")");

						if (rbParent.name.Contains("high"))
						{
							refLength = rbParent.length;
							Debug.Log(refLength);

						}
					}
				}
			}
			bNeedBoneLengths = false;
			Debug.Log("[Skeleton] Updated Bone Length (Skeleton:" + name + ")");
		}
	}
}

// custom skeleton class - used to define bone mapping between a mocap skeleton source (e.g. Arena)
// and a character rig (e.g. standard Autodesk HIK rig)
public class ArenaSkeleton : Skeleton
{
	// IDs from Arena data stream
	public enum ArenaBoneID
	{
		hip = 1,
		abd,
		chest,
		neck,
		head,
		headend,
		lshoulder,
		luarm,
		lfarm,
		lhand,
		lhandend,
		rshoulder,
		ruarm,
		rfarm,
		rhand,
		rhandend,
		lthigh,
		lshin,
		lfoot,
		lfootend,
		rthigh,
		rshin,
		rfoot,
		rfootend
	}

	public static Dictionary<int, string> ArenaToFBX = new Dictionary<int, string>();   // Arena to custom FBX
	public static Dictionary<int, string> ArenaToHIK = new Dictionary<int, string>();   // Arena to standard HIK model

	static ArenaSkeleton()
	{
		// Arena skeleton to standard Maya/Unity HIK model
		ArenaToHIK.Add((int)ArenaBoneID.hip, "Hips");
		ArenaToHIK.Add((int)ArenaBoneID.abd, "Spine");
		ArenaToHIK.Add((int)ArenaBoneID.chest, "Spine2");
		ArenaToHIK.Add((int)ArenaBoneID.neck, "Neck");
		ArenaToHIK.Add((int)ArenaBoneID.head, "Head");
		ArenaToHIK.Add((int)ArenaBoneID.lshoulder, "LeftShoulder");
		ArenaToHIK.Add((int)ArenaBoneID.luarm, "LeftArm");
		ArenaToHIK.Add((int)ArenaBoneID.lfarm, "LeftForeArm");
		ArenaToHIK.Add((int)ArenaBoneID.lhand, "LeftHand");
		ArenaToHIK.Add((int)ArenaBoneID.rshoulder, "RightShoulder");
		ArenaToHIK.Add((int)ArenaBoneID.ruarm, "RightArm");
		ArenaToHIK.Add((int)ArenaBoneID.rfarm, "RightForeArm");
		ArenaToHIK.Add((int)ArenaBoneID.rhand, "RightHand");
		ArenaToHIK.Add((int)ArenaBoneID.lthigh, "LeftUpLeg");
		ArenaToHIK.Add((int)ArenaBoneID.lshin, "LeftLeg");
		ArenaToHIK.Add((int)ArenaBoneID.lfoot, "LeftFoot");
		ArenaToHIK.Add((int)ArenaBoneID.lfootend, "LeftToeBase");
		ArenaToHIK.Add((int)ArenaBoneID.rthigh, "RightUpLeg");
		ArenaToHIK.Add((int)ArenaBoneID.rshin, "RightLeg");
		ArenaToHIK.Add((int)ArenaBoneID.rfoot, "RightFoot");
		ArenaToHIK.Add((int)ArenaBoneID.rfootend, "RightToeBase");


		// Arena to custom FBX model
		// TODO: modify to support your custom FBX character here
		ArenaToFBX.Add((int)ArenaBoneID.hip, "Hips");
		ArenaToFBX.Add((int)ArenaBoneID.abd, "Torso_Lower");
		ArenaToFBX.Add((int)ArenaBoneID.chest, "Torso_Middle");
		ArenaToFBX.Add((int)ArenaBoneID.neck, "Torso_Uppper");
		ArenaToFBX.Add((int)ArenaBoneID.head, "Head");
		ArenaToFBX.Add((int)ArenaBoneID.headend, "HeadEnd");
		ArenaToFBX.Add((int)ArenaBoneID.lshoulder, "LeftShoulder");
		ArenaToFBX.Add((int)ArenaBoneID.luarm, "Bicep_L");
		ArenaToFBX.Add((int)ArenaBoneID.lfarm, "Forearm_L");
		ArenaToFBX.Add((int)ArenaBoneID.lhand, "Wrist_L");
		ArenaToFBX.Add((int)ArenaBoneID.rshoulder, "RightShoulder");
		ArenaToFBX.Add((int)ArenaBoneID.ruarm, "Bicep_R");
		ArenaToFBX.Add((int)ArenaBoneID.rfarm, "Forearm_R");
		ArenaToFBX.Add((int)ArenaBoneID.rhand, "Wrist_R");
		ArenaToFBX.Add((int)ArenaBoneID.lthigh, "Thigh_L");
		ArenaToFBX.Add((int)ArenaBoneID.lshin, "Shin_L");
		ArenaToFBX.Add((int)ArenaBoneID.lfoot, "Foot_L");
		ArenaToFBX.Add((int)ArenaBoneID.lfootend, "Toes_L");
		ArenaToFBX.Add((int)ArenaBoneID.rthigh, "Thigh_R");
		ArenaToFBX.Add((int)ArenaBoneID.rshin, "Shin_R");
		ArenaToFBX.Add((int)ArenaBoneID.rfoot, "Foot_R");
		ArenaToFBX.Add((int)ArenaBoneID.rfootend, "Toes_R");

	}

}

