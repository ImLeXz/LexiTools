#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using VRCSDK2;
using System;

public class FixSkinnedMeshes : ScriptableWizard {
	public GameObject Avatar;
	public Transform anchorOverride;
	public SkinnedMeshRenderer meshToCopy;
	private bool includeInactive = true;
	bool finished;
	public FixSkinnedMeshes() {
		finished = false;
	}

	[MenuItem("Lexi/Set Bounds And Anchors")]
	static void CreateWizard() {
		FixSkinnedMeshes wiz = ScriptableWizard.DisplayWizard<FixSkinnedMeshes>("FixSkinnedMeshes");
	}

    string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
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
            Debug.Log("Error Fixing Skinned Mesh Renderers: " + e);
        }

    }

	void OnGUI() {
		EditorGUILayout.HelpBox ("This will change the anchor override of all skinned mesh renderers on your avatar and make sure all their bounds are the same size", MessageType.Warning);
		Avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", Avatar, typeof(GameObject), true);
		anchorOverride = (Transform)EditorGUILayout.ObjectField("Anchor Override", anchorOverride, typeof(Transform), true);
		meshToCopy = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Mesh To Copy Bounds", meshToCopy, typeof(SkinnedMeshRenderer), true);
		includeInactive = EditorGUILayout.Toggle("Include Inactive?: ", includeInactive);
		if (Avatar && GUILayout.Button ("Fix Skinned Mesh Renderers!")) {
            FixSMR(Avatar, anchorOverride, meshToCopy);
			finished = true;
		}
		if (finished) {
			EditorGUILayout.Space ();
			EditorGUILayout.HelpBox ("Done! :)", MessageType.Info);
		}
	}
}
#endif