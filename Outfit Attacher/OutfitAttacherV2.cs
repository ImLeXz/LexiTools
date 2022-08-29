#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Lexi.OutfitAttacherV2
{
    internal class OutfitAttacherV2 : EditorWindow
    {
        private static OutfitAttacherV2 instance;
        private OutfitAttacherSettings settings;
        private bool bFinished = false;

        internal static OutfitAttacherV2 Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (OutfitAttacherV2)EditorWindow.GetWindow(typeof(OutfitAttacherV2));
                }

                return instance;
            }
        }

        internal struct OutfitAttacherSettings
        {
            internal GameObject avatarRoot; //Very Top Object Of Avatar
            internal GameObject outfitRoot; //Very Top Object Of Outfit

            internal bool bGenerateCopyOfAvatar; //Generates copy of Avatar for backup purposes
            internal bool bGenerateCopyOfOutfit; //Generates copy of Outfit for backup purposes, also needed if prefab not unpacked

            internal bool bAutoCopyComponents;

            internal string outfitName; //Used to create object to store all outfit meshes inside

            internal void InitialiseDefaults()
            {
	            avatarRoot = null;
	            outfitRoot = null;
	            bGenerateCopyOfAvatar = true;
	            bGenerateCopyOfOutfit = true;
	            bAutoCopyComponents = false;
	            outfitName = null;
            }
        }

        [MenuItem("Lexi/Outfit Attacher V2")]
        static void Initialise()
        {
            // Get existing open window or if none, make a new one:
            instance = (OutfitAttacherV2)EditorWindow.GetWindow(typeof(OutfitAttacherV2));
            instance.Show();
        }

        private void Awake()
        {
	        settings = new OutfitAttacherSettings();
	        settings.InitialiseDefaults();
        }

        private void OnGUI()
        {
            string infoMsg =
				"This tool will join two things that have armatures together, whether this be clothing or hairs etc\n\n" +
				"#[Outfit Name] - What You Want To Call The NEW Object The Outfit Meshes Get Parented To" +
				"#[Avatar Root] - Your Avatar\n" +
				"#[Generate Avatar Copy] - Whether To Duplicate Your Avatar (I suggest making your own copy to keep prefab link)\n" +
				"#[Outfit Root] - Your Outfit or Hair etc\n" +
				"#[Generate Outfit Copy] - Whether To Duplicate Your Outfit / Hair\n" +
				"#[AutoCopyComponents] - Attempts To Automatically Copy Any Additional Components On Humanoid Bones (Can Be Scuffed Depending On Outfit)\n";
				
			EditorGUILayout.HelpBox (infoMsg, MessageType.Warning);
			EditorGUILayout.Space(10);

			settings.outfitName = EditorGUILayout.TextField("Outfit Name: ", settings.outfitName);
			EditorGUILayout.Space(10);
			
			settings.avatarRoot = (GameObject)EditorGUILayout.ObjectField("Avatar Root Object: ", settings.avatarRoot,
				typeof(GameObject), true);
			settings.bGenerateCopyOfAvatar = EditorGUILayout.Toggle("Generate Avatar Copy? ", settings.bGenerateCopyOfAvatar);
			EditorGUILayout.Space(10);

			settings.outfitRoot = (GameObject)EditorGUILayout.ObjectField("Outfit Root Object: ", settings.outfitRoot,
				typeof(GameObject), true);
			settings.bGenerateCopyOfOutfit = EditorGUILayout.Toggle("Generate Outfit Copy? ", settings.bGenerateCopyOfOutfit);
			EditorGUILayout.Space(10);
			
			settings.bAutoCopyComponents = EditorGUILayout.Toggle("Auto Copy Components? ", settings.bAutoCopyComponents);
			EditorGUILayout.Space(10);

			if (settings.avatarRoot && settings.outfitRoot && GUILayout.Button("Attach Outfit!"))
			{
				LexiSkinnedMeshCombiner.CombineSkinnedMesh(settings);
				bFinished = true;
			}

			if (bFinished)
			{
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Done! :)", MessageType.Info);
			}
        }
    }
}

#endif
