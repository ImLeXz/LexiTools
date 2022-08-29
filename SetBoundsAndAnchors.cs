#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using VRCSDK2;
using System;

public class SetBoundsAndAnchors : ScriptableWizard {
	public GameObject Avatar;
	public Transform anchorOverride;
	public SkinnedMeshRenderer meshToCopy;
	private bool includeInactive = true;
	bool finished;
	public SetBoundsAndAnchors() {
		finished = false;
	}

	[MenuItem("Lexi/Set Bounds And Anchors")]
	static void CreateWizard() {
		SetBoundsAndAnchors wiz = ScriptableWizard.DisplayWizard<SetBoundsAndAnchors>("SetBoundsAndAnchors");
	}
	

    void FixSMR(GameObject obj, Transform anchor, SkinnedMeshRenderer main) 
	{
        try
        {
            SkinnedMeshRenderer[] smr = obj.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive);
            Debug.Log("Got Skinned Mesh Renderers: " + smr.Length);
            for (int i = 0; i < (int)smr.Length; i++)
            {
                SkinnedMeshRenderer currentSMR = smr[i];
                Debug.Log("Properties Set For: " + currentSMR.gameObject.name);
				currentSMR.localBounds = main.localBounds;
				currentSMR.probeAnchor = anchor;
            }
        }
        catch(Exception e)
        {
            Debug.Log("Error Settings Bounds and Anchors: " + e);
        }

    }

	void OnGUI() {
		EditorGUILayout.HelpBox ("This will change the anchor override of all skinned mesh renderers on your avatar and make sure all their bounds are the same size", MessageType.Warning);
		Avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", Avatar, typeof(GameObject), true);
		anchorOverride = (Transform)EditorGUILayout.ObjectField("Anchor Override", anchorOverride, typeof(Transform), true);
		meshToCopy = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Mesh To Copy Bounds", meshToCopy, typeof(SkinnedMeshRenderer), true);
		includeInactive = EditorGUILayout.Toggle("Include Inactive?: ", includeInactive);
		if (Avatar && GUILayout.Button ("Set Bounds And Anchors!")) {
            FixSMR(Avatar, anchorOverride, meshToCopy);
			finished = true;
		}
		if (finished) {
			EditorGUILayout.Space ();
			EditorGUILayout.HelpBox ("Done! :)", MessageType.Info);
			finished = false;
		}
	}
}
#endif