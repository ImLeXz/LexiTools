#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using Lexi.HelperFunctions;
using UnityEngine;

namespace Lexi.OutfitAttacherV2
{
    internal static class LexiSkinnedMeshCombiner
    {
        private const string duplicateObjTag = "(Duplicate)";
        
        private const string W_NoOutfitName = "No Outfit Name Specified, Joining Meshes To AvatarRoot Instead";
        private const string E_CopiedBonesNull = "Copied Bones Was NULL Or Empty";

        private const bool bDoCleanup = true;

        private static List<GameObject> copiedBones;
        private static List<GameObject> copiedObjects;
        private static List<GameObject> duplicateObjects;

        internal static void CombineSkinnedMesh(OutfitAttacherV2.OutfitAttacherSettings outfitAttacherSettings)
        {
            #region PreSetup

            copiedBones = new List<GameObject>();
            copiedObjects = new List<GameObject>();
            duplicateObjects = new List<GameObject>();
            
            GameObject avatarRoot = outfitAttacherSettings.avatarRoot;
            GameObject outfitRoot = outfitAttacherSettings.outfitRoot;
            if (outfitAttacherSettings.bGenerateCopyOfAvatar) avatarRoot = DuplicateObj(avatarRoot);
            if (outfitAttacherSettings.bGenerateCopyOfOutfit) outfitRoot = DuplicateObj(outfitRoot);
            
            CopyTransform(avatarRoot.transform, outfitRoot.transform);

            GameObject outfitParent = avatarRoot;
            if (!string.IsNullOrEmpty(outfitAttacherSettings.outfitName))
            {
                outfitParent = GenerateDefaultGameObject(outfitAttacherSettings.outfitName, avatarRoot.transform);
            }
            else Debug.LogWarning(W_NoOutfitName);
            
            #endregion
            
            SkinnedMeshRenderer[] outfitMeshes = outfitRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Debug.Log("Attempting To Attach ["+outfitMeshes.Length+"] Meshes To ["+avatarRoot.name+"]");

            for (int i = 0; i < outfitMeshes.Length; i++)
            {
                Debug.Log( "["+i+"] Joining Mesh: "+outfitMeshes[i].gameObject.name);
                outfitMeshes[i].bones = MergeBones(outfitMeshes[i], avatarRoot, outfitAttacherSettings);
                SetRootBone(outfitMeshes[i], avatarRoot);
                
                outfitMeshes[i].transform.SetParent(outfitParent.transform);
                outfitMeshes[i].transform.localPosition = Vector3.zero;
                outfitMeshes[i].transform.localRotation = Quaternion.identity;
                outfitMeshes[i].transform.localScale = Vector3.one;
            }
            
            CopyExtraGameObjects(copiedBones, avatarRoot, outfitAttacherSettings); //Copies Stuff Like Additional Colliders Etc
            if(outfitAttacherSettings.bAutoCopyComponents) CopyComponents(outfitRoot, avatarRoot, outfitAttacherSettings);

            //Delete Outfit Copy As No Longer Needed
            if (outfitAttacherSettings.bGenerateCopyOfOutfit && bDoCleanup)
            {
                Debug.Log("Destroying Outfit Copy...");
                GameObject.DestroyImmediate(outfitRoot);
            }
        }

        private static void CopyComponents(GameObject outfitRoot, GameObject avatarRoot, OutfitAttacherV2.OutfitAttacherSettings outfitAttacherSettings)
        {
            //Loop through all left over objects in outfit hierarchy
            //If object has component that isnt transform, add it to dictionary with its original path

            Dictionary<GameObject, string> objectsToTransfer = new Dictionary<GameObject, string>();
            Transform[] allChildren = outfitRoot.GetComponentsInChildren<Transform>(true);
            Debug.Log("-----[Remaining Objects On Outfit]-----");
            for (int i = 0; i < allChildren.Length; i++)
            {
                Component[] allComponentsOnChild = allChildren[i].GetComponents(typeof(Component));
                for (int j = 0; j < allComponentsOnChild.Length; j++)
                {
                    if(allComponentsOnChild[j].GetType() == typeof(Transform)) continue; //Skip If Transform Component
                    Debug.Log("["+allChildren[i].gameObject.name+"] - ["+allComponentsOnChild[j]+"]");
                    if (!objectsToTransfer.ContainsKey(allChildren[i].gameObject))
                    {
                        objectsToTransfer.Add(allChildren[i].gameObject, Helpers.GetGameObjectPath(allChildren[i].gameObject));
                    }
                }
            }

            //Loop through dictionary, Get transform in other hierarchy using saved path
            //Generate new parent bone with matching transform as parent, replace old parent bone with new if names match
            foreach(KeyValuePair<GameObject, string> objToTransfer in objectsToTransfer)
            {
                Transform objTransform_OnAvatar = Helpers.FindTransformInAnotherHierarchy(objToTransfer.Key.transform,
                    avatarRoot.transform, false, objToTransfer.Value);
                
                if (objTransform_OnAvatar == null)
                {
                    Debug.LogError("Could Not Find ["+objToTransfer.Key.name+"] At - ["+objToTransfer.Value+"]");
                    continue;
                }
                
                SetParentWithoutChildren(objToTransfer.Key.transform, objTransform_OnAvatar);
                objToTransfer.Key.transform.localPosition = Vector3.zero;
                objToTransfer.Key.transform.localRotation = Quaternion.identity;
                objToTransfer.Key.transform.localScale = Vector3.one;
                
                string outfitParentName = outfitAttacherSettings.outfitName + " [" + objTransform_OnAvatar.gameObject.name + "]";
                GameObject outfitParent = GenerateDefaultGameObject(outfitParentName, objTransform_OnAvatar);
                ReplaceGameObject(outfitParent, objToTransfer.Key, true);
            }
        }

        private static void SetParentWithoutChildren(Transform objToChange, Transform newParent)
        {
            GameObject dupeObj = new GameObject(objToChange.name + " (Clone)");
            dupeObj.transform.parent = objToChange.parent;
            dupeObj.transform.localPosition = objToChange.transform.localPosition;
            dupeObj.transform.localRotation = objToChange.transform.localRotation;
            dupeObj.transform.localScale = objToChange.transform.localScale;

            int childCount = objToChange.childCount;
            List<Transform> objectsToMove = new List<Transform>();
            for (int i = 0; i < childCount; i++)
            {
                Transform child = objToChange.GetChild(i);
                objectsToMove.Add(child);
            }
            
            foreach(Transform t in objectsToMove) t.SetParent(dupeObj.transform);

            objToChange.SetParent(newParent);
        }

        private static void SetRootBone(SkinnedMeshRenderer currentSkinnedMesh, GameObject otherHierarchy)
        {
            Transform newRootBone = Helpers.FindTransformInAnotherHierarchy(currentSkinnedMesh.rootBone, 
                otherHierarchy.transform, false);

            if (newRootBone != null)
            {
                currentSkinnedMesh.rootBone = newRootBone;
            }
        }

        private static Transform[] MergeBones(SkinnedMeshRenderer currentSkinnedMesh, GameObject otherHierarchy, OutfitAttacherV2.OutfitAttacherSettings outfitAttacherSettings)
        {
            Transform[] outfitBones = currentSkinnedMesh.bones;
            Transform[] outfitBones_OnAvatar = new Transform[outfitBones.Length];

            for (int i = 0; i < outfitBones.Length; i++)
            {
                //Checks if bone is null since meshes can get scuffed and have null bones
                if (outfitBones[i] == null)
                {
                    Debug.LogError("Bone ["+i+"] For Mesh ["+currentSkinnedMesh.gameObject.name+"] Was NULL");
                    continue; //Moves To Next ForLoop Iteration
                }
                
                //Tries to find same bone from outfit on avatar
                Transform outfitBone_OnAvatar = Helpers.FindTransformInAnotherHierarchy(outfitBones[i], 
                    otherHierarchy.transform, false);
                
                #region New Bone Generation
                if (outfitBone_OnAvatar == null) //If couldn't find same bone
                {
                    Debug.LogWarning("Could Not Find Bone [" + outfitBones[i].name + "] In [" + otherHierarchy.name +
                                     "] -> Generating New Bone");
                    outfitBone_OnAvatar =
                        GenerateNewBoneOnAvatar(outfitBones[i], otherHierarchy.transform, outfitAttacherSettings);
                }
                #endregion
                
                //Stores new bone into array, to be used for setting outfit bones later
                outfitBones_OnAvatar[i] = outfitBone_OnAvatar;

                if (!copiedBones.Contains(outfitBones[i].gameObject))
                {
                    copiedBones.Add(outfitBones[i].gameObject);
                }
            }

            return outfitBones_OnAvatar;
        }
        
        private static void CopyExtraGameObjects(List<GameObject> copiedBones, GameObject otherHierarchy, OutfitAttacherV2.OutfitAttacherSettings outfitAttacherSettings)
		{
			GameObject[] objectsToCopy = GetObjectsToCopy(copiedBones);
            if (objectsToCopy == null || objectsToCopy.Length <= 0) return;
            
			Debug.Log("Copying Extra Objects");
			for (int i = 0; i < objectsToCopy.Length; i++)
			{
				Debug.Log("["+i+"] Attempting To Copy ["+objectsToCopy[i].name+"]");
                GenerateNewBoneOnAvatar(objectsToCopy[i].transform, otherHierarchy.transform, outfitAttacherSettings);
            }
			
			Debug.Log("---[Printing Results For Dupes]---");
			PrintResults(duplicateObjects, "Duplicate Object, Not Copied");
			
			Debug.Log("---[Printing Results For Uniques]---");
			PrintResults(copiedObjects, "Unique Object, Copied");
		}
        
        private static void PrintResults(List<GameObject> objects, string message)
        {
	        foreach (var obj in objects)
	        {
		        Debug.Log(obj + " - " + message);
	        }	
        }

        private static GameObject[] GetObjectsToCopy(List<GameObject> copiedBones)
        {
            if (copiedBones == null || copiedBones.Count <= 0)
            {
                Debug.LogError(E_CopiedBonesNull);
                return null;
            }
            
	        List<GameObject> objectsToCopy = new List<GameObject>();
            for (int i = 0; i < copiedBones.Count; i++)
            {
                if (copiedBones[i] == null)
                {
                    Debug.LogError("Copied Bone ["+i+"] Was NULL");
                    continue;
                }
                
                int childCount = copiedBones[i].transform.childCount;
		        for(int j = 0; j < childCount; j++)
                {
                    Transform child = copiedBones[i].transform.GetChild(j);
                    
                    if (child == null)
                    {
                        Debug.LogError("Child ["+j+"] Of ["+copiedBones[i].name+" Was NULL");
                        continue;
                    }
                    
                    if (!copiedBones.Contains(child.gameObject) && !objectsToCopy.Contains(child.gameObject)) //Makes sure child is not one of parent bones and child is not already copied
			        {
				        objectsToCopy.Add(child.gameObject);
			        }
		        }
	        }

            return objectsToCopy.ToArray();
        }

        private static Transform GenerateNewBoneOnAvatar(Transform outfitBone, Transform otherHierarchy,
            OutfitAttacherV2.OutfitAttacherSettings outfitAttacherSettings)
        {
            Transform outfitBone_OnAvatar = outfitBone; //Sets new bone to old bone as there is no new bone
            Transform outfitBoneParent_OnAvatar = Helpers.FindTransformInAnotherHierarchy(outfitBone.parent,
                otherHierarchy.transform, false);

            if (outfitBoneParent_OnAvatar == null) //If couldn't find same parent name
            {
                string objPath = Helpers.GetGameObjectPath(outfitBone.parent.gameObject);
                Debug.LogWarning("Could Not Find Parent [" + objPath + "] In [" + otherHierarchy.transform + "] -> Generating New Parent");
                outfitBoneParent_OnAvatar =
                    GenerateNewBoneOnAvatar(outfitBone.parent, otherHierarchy, outfitAttacherSettings);
            }

            string outfitParentName = outfitAttacherSettings.outfitName + " [" + outfitBoneParent_OnAvatar.gameObject.name + "]";
            GameObject outfitParentObj = GenerateDefaultGameObject(outfitParentName, outfitBoneParent_OnAvatar);

            //If generated OutfitParentObj, then set the outfitBoneParent to the generated object
            if (outfitParentObj != null) outfitBoneParent_OnAvatar = outfitParentObj.transform;

            //Set parent of new bone to new parent and make sure transforms match
            Quaternion previousRotation = outfitBone_OnAvatar.localRotation;
            Vector3 previousScale = outfitBone_OnAvatar.localScale;
            Vector3 previousPosition = outfitBone_OnAvatar.localPosition;

            if (outfitBoneParent_OnAvatar != null)
            {
                outfitBone_OnAvatar.SetParent(outfitBoneParent_OnAvatar);
            }

            copiedObjects.Add(outfitBone_OnAvatar.gameObject);

            outfitBone_OnAvatar.localRotation = previousRotation;
            outfitBone_OnAvatar.localPosition = previousPosition;
            outfitBone_OnAvatar.localScale = previousScale;
            return outfitBone_OnAvatar;
        }

        private static GameObject GenerateDefaultGameObject(string objName, Transform parent, bool replaceIfDupe = false)
        {
            GameObject duplicateObj = null;
            int childCount = parent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                string childName = parent.GetChild(i).gameObject.name;
                if (childName == objName)
                {
                    duplicateObj = parent.GetChild(i).gameObject;
                    if(!replaceIfDupe) return duplicateObj;
                }
            }
            
            GameObject generatedObject = new GameObject(objName);
            generatedObject.transform.SetParent(parent.transform);
            generatedObject.transform.localPosition = Vector3.zero;
            generatedObject.transform.localRotation = Quaternion.identity;
            generatedObject.transform.localScale = Vector3.one;

            if (duplicateObj != null && replaceIfDupe)
            {
                ReplaceGameObject(duplicateObj, generatedObject, true);
            }

            return generatedObject;
        }

        private static void ReplaceGameObject(GameObject oldObj, GameObject newObj, bool renameNewToOld)
        {
            int childCount = oldObj.transform.childCount;
            List<Transform> childObjs = new List<Transform>();
            for (int i = 0; i < childCount; ++i)
            {
                Transform child = oldObj.transform.GetChild(i);
                childObjs.Add(child);
            }

            foreach (var child in childObjs) child.SetParent(newObj.transform);
            
            newObj.name = oldObj.name;
            if(bDoCleanup) GameObject.DestroyImmediate(oldObj);
        }

        private static void CopyTransform(Transform from, Transform to)
        {
            to.position = from.position;
            to.rotation = from.rotation;
            to.localScale = from.localScale;
        }

        private static GameObject DuplicateObj(GameObject obj)
        {
            GameObject duplicateObj = GameObject.Instantiate(obj);
            duplicateObj.name = obj.name + " " + duplicateObjTag;
            obj.SetActive(false);
            return duplicateObj;
        }
    }
}

#endif