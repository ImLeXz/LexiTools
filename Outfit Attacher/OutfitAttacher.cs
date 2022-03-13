
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Lexi.HelperFunctions;

namespace Lexi.OutfitAttacher //Written By ImLeXz#1234 (Discord)
{
	public class OutfitAttacher : ScriptableWizard
	{
		public string outfitName = "";
		public GameObject avatarRootObj = null;
		public GameObject outfitRootObj = null;

		public float rigMatchThreshold = 0.05f;
		public bool matchToChangedRig = true;
		
		public bool generateAvatarCopy = true;
		public bool generateOutfitCopy = true;
		
		public bool generateNewParentBones = true;
		public bool keepDuplicateExtras = false;
		
		bool finished = false;
		private List<GameObject> copiedObjects;
		private List<GameObject> duplicateObjects;
		private Dictionary<string, GameObject> generatedParents;
		
		
		public OutfitAttacher()
		{
			finished = false;
		}

		[MenuItem("Lexi/Outfit Attacher")]
		static void CreateWizard()
		{
			OutfitAttacher wiz = ScriptableWizard.DisplayWizard<OutfitAttacher>("Attach Separate Outfit To Avatar");
		}

		void AttachOutfit()
		{
			copiedObjects = new List<GameObject>();
			duplicateObjects = new List<GameObject>();
			generatedParents = new Dictionary<string, GameObject>();

			#region Generate Copies
			GameObject outfitCopy = outfitRootObj;
			if (generateOutfitCopy) outfitCopy = Instantiate(outfitRootObj);

			if (PrefabUtility.IsPartOfAnyPrefab(outfitCopy))
			{
				Debug.LogError("Cannot Proceed As Outfit Is Part Of Prefab, Please Unpack Prefab Before " +
				               "Running Script Again!");
				return;
			}
			
			GameObject avatarCopy = avatarRootObj;
			if (generateAvatarCopy)
			{
				avatarCopy = Instantiate(avatarRootObj);
				avatarCopy.name = avatarRootObj.name + " - (Outfit Attached)";
			}

			outfitCopy.transform.localScale = avatarCopy.transform.localScale;
			#endregion
			
			List<GameObject> copiedBones = new List<GameObject>();
			SkinnedMeshRenderer[] outfitMeshes = outfitCopy.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			GameObject outfitParent = avatarCopy;
			if (!string.IsNullOrEmpty(outfitName))
			{
				outfitParent = new GameObject(outfitName);
				outfitParent.transform.SetParent(avatarCopy.transform);
				outfitParent.transform.localPosition = Vector3.zero;
				outfitParent.transform.localRotation = Quaternion.identity;
				outfitParent.transform.localScale = Vector3.one;
			}

			Debug.Log("Attempting To Attach [" + outfitMeshes.Length + "] Meshes");

			#region Skinned Mesh Iteration
			
			foreach (SkinnedMeshRenderer outfitSkinnedMesh in outfitMeshes)
			{
				Debug.Log("Doing Stuff For: " + outfitSkinnedMesh.gameObject.name);

				Transform[] outfitBones = outfitSkinnedMesh.bones;
				Transform[] newOutfitBones = new Transform[outfitBones.Length];

				#region Armature Iteration
				
				for (int i = 0; i < outfitBones.Length; i++)
				{
					//Checks if bone is null since meshes can get scuffed and have null bones
					if (outfitBones[i] == null)
					{
						Debug.LogError("Bone For Mesh - " + outfitSkinnedMesh.gameObject.name + " - Was NULL");
						break;
					}

					//Tries to find same bone from outfit on avatar
					Transform newBoneTransform = Helpers.FindTransformInAnotherHierarchy(outfitBones[i], 
						avatarCopy.transform, false);
					//Transform newBoneTransform = Helpers.RecurseFind(avatarCopy.transform, outfitBones[i].name);
					
					#region New Bone Generation
					//If couldn't find same bone
					if (newBoneTransform == null)
					{
						Debug.Log("Could Not Find Bone (" + outfitBones[i].name + ") In " + avatarCopy.name);
						newBoneTransform = outfitBones[i]; //Sets new bone to old bone as there is no new bone

						//Gets parent of new bone on avatar by finding same bone name as outfit bone parent.
						
						//Transform newBoneParent = Helpers.RecurseFind(avatarCopy.transform, outfitBones[i].parent.name);
						Transform newBoneParent = Helpers.FindTransformInAnotherHierarchy(outfitBones[i].parent, 
							avatarCopy.transform, false);
						
						if (generateNewParentBones)
						{
							newBoneParent = GenerateNewParent(newBoneParent).transform;
						}

						if (newBoneParent == null)
						{
							//If couldn't find same parent name
							Debug.LogError("Could Not Find Parent: " + outfitBones[i].parent.name);
						}

						//Set parent of new bone to new parent and make sure transforms match
						Quaternion previousRotation = newBoneTransform.localRotation;
						Vector3 previousScale = newBoneTransform.localScale;
						Vector3 previousPosition = newBoneTransform.localPosition;
						newBoneTransform.SetParent(newBoneParent);
						Debug.Log("Set Parent Of [" + newBoneTransform.gameObject.name + "] To [" +
						          newBoneParent.gameObject.name + "]");
						copiedObjects.Add(newBoneTransform.gameObject);

						newBoneTransform.localRotation = previousRotation;
						newBoneTransform.localPosition = previousPosition;
						newBoneTransform.localScale = previousScale;
					}
					#endregion
					
					#region Not Copied Debug Log
					else
					{
						Component[] boneComponents = outfitBones[i].GetComponents<Component>();
						foreach (var boneComponent in boneComponents)
						{
							System.Type t = boneComponent.GetType();
							if (t != typeof(Transform))
							{
								Debug.LogWarning(outfitBones[i].gameObject.name + 
								               " Bone Not Copied With Following Component: " + t);
							}
						}
					}
					#endregion
					
					//Stores new bone into array, to be used for setting outfit bones later
					newOutfitBones[i] = newBoneTransform;

					if (!copiedBones.Contains(outfitBones[i].gameObject))
					{
						copiedBones.Add(outfitBones[i].gameObject);
					}
				}
				
				#endregion

				Transform newRootBone = Helpers.FindTransformInAnotherHierarchy(outfitSkinnedMesh.rootBone, 
					avatarCopy.transform, false);
				//Transform newRootBone = Helpers.RecurseFind(avatarCopy.transform, outfitSkinnedMesh.rootBone.name);
				
				if (newRootBone != null)
				{
					outfitSkinnedMesh.rootBone = newRootBone;
				}
				
				outfitSkinnedMesh.bones = newOutfitBones;
				outfitSkinnedMesh.transform.SetParent(outfitParent.transform);
				
				outfitSkinnedMesh.transform.localPosition = Vector3.zero;
				outfitSkinnedMesh.transform.localRotation = Quaternion.identity;
				outfitSkinnedMesh.transform.localScale = Vector3.one;
			}
			
			#endregion

			CopyExtraGameObjects(copiedBones, avatarCopy);
			
			//Final Cleanup
			avatarRootObj.SetActive(!generateAvatarCopy);
			outfitRootObj.SetActive(!generateOutfitCopy);
			DestroyImmediate(outfitCopy);
		}

		private bool DoTransformsMatch(Transform t1, Transform t2)
		{
			/*
			float rotationDifferenceX = GetDifference(t1.localEulerAngles.x, t2.localEulerAngles.x);
			float rotationDifferenceY = GetDifference(t1.localEulerAngles.y, t2.localEulerAngles.y);
			float rotationDifferenceZ = GetDifference(t1.localEulerAngles.z, t2.localEulerAngles.z);
			
			float positionDifferenceX = GetDifference(t1.localPosition.x, t2.localPosition.x);
			float positionDifferenceY = GetDifference(t1.localPosition.y, t2.localPosition.y);
			float positionDifferenceZ = GetDifference(t1.localPosition.z, t2.localPosition.z);
			
			float scaleDifferenceX = GetDifference(t1.localScale.x, t2.localScale.x);
			float scaleDifferenceY = GetDifference(t1.localScale.y, t2.localScale.y);
			float scaleDifferenceZ = GetDifference(t1.localScale.z, t2.localScale.z);
			*/
			
			float positionDifference = Vector3.Distance(t1.position, t2.position);
			float rotationDifference = Quaternion.Angle(t1.rotation, t2.rotation);
			float scaleDifference = Vector3.Distance(t1.localScale, t2.localScale);

			if (positionDifference > rigMatchThreshold)
			{
				Debug.Log("Position Difference Between [" + t1.gameObject.name + "] & [" + t2.gameObject.name + "] " +
				          "Was: " + positionDifference);

				return false;
			}

			if (scaleDifference > rigMatchThreshold)
			{
				Debug.Log("Rotation Difference Between [" + t1.gameObject.name + "] & [" + t2.gameObject.name + "] " +
				          "Was: " + rotationDifference);

				return false;
			}

			if (rotationDifference > rigMatchThreshold)
			{
				Debug.Log("Scale Difference Between [" + t1.gameObject.name + "] & [" + t2.gameObject.name + "] " +
				          "Was: " + scaleDifference);
				
				return false;
			} 
			
			return true;
		}
		private float GetDifference(float f1, float f2)
		{
			return Mathf.Abs(f1 - f2);
		}
		
		private GameObject GenerateNewParent(Transform previousParent)
		{
			string newParentName = (outfitName + " - " + previousParent.gameObject.name);
			if (generatedParents.ContainsKey(newParentName)) return generatedParents[newParentName];
			
			Debug.Log("Creating New Parent With Name: " + newParentName);
			GameObject newParentObj = new GameObject();
			newParentObj.name = newParentName;
			newParentObj.transform.SetParent(previousParent);
			newParentObj.transform.localScale = Vector3.one;
			newParentObj.transform.localPosition = Vector3.zero;
			newParentObj.transform.localRotation = Quaternion.identity;
			generatedParents.Add(newParentName, newParentObj);
			return newParentObj;
		}

		private void CopyExtraGameObjects(List<GameObject> copiedBones, GameObject avatarCopy)
		{
			List<GameObject> objectsToCopy = new List<GameObject>();
			GetObjectsToCopy(objectsToCopy, copiedBones);

			foreach (var objToCopy in objectsToCopy)
			{
				Debug.Log("Attempting To Copy [" + objToCopy.name + "] To [" + objToCopy.transform.parent.name + "]");

				Transform newParent = Helpers.FindTransformInAnotherHierarchy(objToCopy.transform.parent, 
					avatarCopy.transform, false);
				//Transform newParent = Helpers.RecurseFind(avatarCopy.transform, objToCopy.transform.parent.name);
				
				
				if (newParent == null)
				{
					Debug.LogError("Couldn't Find New Parent For: " + objToCopy.name);
				}
				
				else
				{
					Quaternion previousRotation = objToCopy.transform.localRotation;
					Vector3 previousScale = objToCopy.transform.localScale;
					Vector3 previousPosition = objToCopy.transform.localPosition;

					if (keepDuplicateExtras)
					{
						if (generateNewParentBones) newParent = GenerateNewParent(newParent).transform;
						objToCopy.transform.SetParent(newParent);
					}
					
					else
					{
						Transform duplicateTransform = Helpers.FindTransformInAnotherHierarchy(objToCopy.transform, 
							avatarCopy.transform, false);
						if (duplicateTransform != null)
						{
							duplicateObjects.Add(objToCopy);
						}
						else
						{
							if (generateNewParentBones) newParent = GenerateNewParent(newParent).transform;
							objToCopy.transform.SetParent(newParent);
							copiedObjects.Add(objToCopy);
						}
					}
					
					objToCopy.transform.localRotation = previousRotation;
					objToCopy.transform.localPosition = previousPosition;
					objToCopy.transform.localScale = previousScale;
					
					copiedBones.Add(objToCopy);
				}
			}
			
			Debug.Log("---[Printing Results For Dupes]---");
			PrintResults(duplicateObjects, "Duplicate Object, Not Copied");
			
			Debug.Log("---[Printing Results For Uniques]---");
			PrintResults(copiedObjects, "Unique Object, Copied");
		}

		void PrintResults(List<GameObject> objects, string message)
		{
			foreach (var obj in objects)
			{
				Debug.Log(obj + " - " + message);
			}	
		}

		void GetObjectsToCopy(List<GameObject> objList, List<GameObject> copiedList)
		{
			foreach (GameObject obj in copiedList) //Parent Bones
			{
				foreach (Transform t in obj.transform) //Children
				{
					if (!copiedList.Contains(t.gameObject) && !objList.Contains(t.gameObject)) //Makes sure child is not one of parent bones and child is not already copied
					{
						objList.Add(t.gameObject);
					}
				}
			}
		}

		void OnGUI()
		{
			string infoMsg =
				"This tool will join two things that have armatures together, whether this be clothing or hairs etc\n\n" +
				"#[Outfit Name] - What You Want To Call The NEW Object The Outfit Meshes Get Parented To" +
				"#[Avatar Root] - Your Avatar\n" +
				"#[Generate Avatar Copy] - Whether To Duplicate Your Avatar (I suggest making your own copy to keep prefab link)\n" +
				"#[Outfit Root] - Your Outfit or Hair etc\n" +
				"#[Generate Outfit Copy] - Whether To Duplicate Your Outfit / Hair\n" +
				"#[Keep Duplicate Extras] - Whether To Keep Same Named Things Such As DynamicBone Colliders, Or To Use Ones Already On The Model\n" +
				"#[Generate New Parent Bones] - This Will Create A New Object That All Extra Objects (DB Colliders Etc) Will Be Parented Under, Making It Easier To Toggle Dynamic Bones With The Outfit\n";
				
			EditorGUILayout.HelpBox (infoMsg, MessageType.Warning);
			EditorGUILayout.Space(10);

			outfitName = EditorGUILayout.TextField("Outfit Name: ", outfitName);
			EditorGUILayout.Space(10);
			
			avatarRootObj = (GameObject)EditorGUILayout.ObjectField("Avatar Root Object: ", avatarRootObj,
				typeof(GameObject), true);
			generateAvatarCopy = EditorGUILayout.Toggle("Generate Avatar Copy? ", generateAvatarCopy);
			EditorGUILayout.Space(10);

			outfitRootObj = (GameObject)EditorGUILayout.ObjectField("Outfit Root Object: ", outfitRootObj,
				typeof(GameObject), true);
			generateOutfitCopy = EditorGUILayout.Toggle("Generate Outfit Copy? ", generateOutfitCopy);
			EditorGUILayout.Space(10);
			
			keepDuplicateExtras = EditorGUILayout.Toggle("Keep Duplicated Extra Objects? ", keepDuplicateExtras);
			generateNewParentBones = EditorGUILayout.Toggle("Generate New Parent Bones? ", generateNewParentBones);
			EditorGUILayout.Space(10);

			if (avatarRootObj && outfitRootObj && GUILayout.Button("Attach Outfit!"))
			{
				AttachOutfit();
				finished = true;
			}

			if (finished)
			{
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Done! :)", MessageType.Info);
			}
		}
		
	}
}

#endif