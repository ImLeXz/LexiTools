#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace Lexi
{
    public class GenerateMatPropertyAnimations : ScriptableWizard
    {
        //Current names of properties to animate in poi shader
        public struct AnimatableProperty
        {
            public string animationName;
            public string propertyName;
            public float minValue;
            public float midValue;
            public float maxValue;
        }

        public AnimatableProperty aprop_lightingMultiplier;
        public AnimatableProperty aprop_emisisonMultiplier;
        public AnimatableProperty aprop_monochromaticMultiplier;
        public AnimatableProperty aprop_monochromaticAdditiveMultiplier;

        public GameObject avatar;
        public string materialAnimationName;
        public string materialPropertyName;
        public float propertyMinValue = 0.0f;
        public float propertyMidValue = 0.5f;
        public float propertyMaxValue = 1.0f;
        public string animatableSuffix = "Animated";
        public string shaderTag = ".poiyomi";
        
        AnimationClip materialAdjustAnim = null;
        bool finished;
        private bool propertiesRegistered = false;
        const string animationPropertySuffix = "material.";

        private List<Renderer> renderersAnimated = new List<Renderer>();
        
        //Needed To Set LoopTime cos Unity Poopoo
        class AnimationClipSettings
        {
            SerializedProperty m_Property;

            private SerializedProperty Get(string property) { return m_Property.FindPropertyRelative(property); }

            public AnimationClipSettings(SerializedProperty prop) { m_Property = prop; }

            public bool loopTime { get { return Get("m_LoopTime").boolValue; } set { Get("m_LoopTime").boolValue = value; } }
        }

        public GenerateMatPropertyAnimations()
        {
            finished = false;
        }

        [MenuItem("Lexi/Generate Material Anims")]
        static void CreateWizard()
        {
            GenerateMatPropertyAnimations wiz = ScriptableWizard.DisplayWizard<GenerateMatPropertyAnimations>("Generate Material Animations");
        }

        Transform RecurseFind(Transform obj, string name)
        {
            Transform find = obj.Find(name);

            if (find)
                return find;

            for (int i = 0; i < obj.childCount; i++)
            {
                find = RecurseFind(obj.GetChild(i), name);
                if (find)
                    return find;
            }
            return null;
        }

        private static string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                if(transform.parent != null)
                    path = transform.name + "/" + path;
            }
            return path;
        }

        void GenerateAnims(AnimatableProperty animatableProperty)
        {
            try
            {
                Renderer[] meshes = avatar.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < (int)meshes.Length; i++)
                {
                    Type meshType = meshes[i].GetType();
                    if (meshType == typeof(MeshRenderer) || meshType == typeof(SkinnedMeshRenderer))
                    {
                        bool shouldProcessMesh = true;
                        if (!String.IsNullOrEmpty(shaderTag))
                        {
                            foreach (var VARIABLE in meshes[i].sharedMaterials)
                            {
                                if(VARIABLE is null) continue;
                                
                                if (!VARIABLE.shader.name.Contains(shaderTag)) continue;
                                if (!String.IsNullOrEmpty(animatableSuffix))
                                {
                                    Debug.Log("Setting - " + animatableProperty.propertyName + " - On - " + VARIABLE.name + " - To Animated");
                                    string animatedOverrideTag = animatableProperty.propertyName + animatableSuffix;
                                    //Debug.Log("Using Override Tag: " + animatedOverrideTag);
                                    VARIABLE.SetOverrideTag(animatedOverrideTag, "1");
                                }
                            }
                        }

                        if (renderersAnimated.Contains(meshes[i])) shouldProcessMesh = false;
                        else
                        {
                            renderersAnimated.Add(meshes[i]);
                            shouldProcessMesh = true;
                        }

                        if (!shouldProcessMesh)
                        {
                            Debug.LogWarning("Couldn't Find Any Materials With Shader Tag: " + shaderTag);
                            continue;
                        }
                        Debug.Log("Generating KeyFrames For: " + meshes[i].name);
                        Transform meshTransform = meshes[i].gameObject.transform;

                        AnimationCurve animationCurve = new AnimationCurve();
                        animationCurve.AddKey(0.0f, animatableProperty.minValue);
                        animationCurve.AddKey(0.25f, (animatableProperty.minValue + animatableProperty.midValue) / 2 );
                        animationCurve.AddKey(0.5f, animatableProperty.midValue);
                        animationCurve.AddKey(0.75f, (animatableProperty.maxValue + animatableProperty.midValue) / 2 );
                        animationCurve.AddKey(1.0f, animatableProperty.maxValue);
                        
                        string animatedPropertyName = animationPropertySuffix + animatableProperty.propertyName;
                        materialAdjustAnim.SetCurve(GetGameObjectPath(meshTransform), meshType, animatedPropertyName,
                            animationCurve);

                        if (animatableProperty.propertyName == aprop_monochromaticMultiplier.propertyName)
                        {
                            animatedPropertyName = animationPropertySuffix + aprop_monochromaticAdditiveMultiplier.propertyName;
                            materialAdjustAnim.SetCurve(GetGameObjectPath(meshTransform), meshType, animatedPropertyName,
                                animationCurve);
                        }
                    }
                }

                materialAdjustAnim.wrapMode = WrapMode.Clamp;
                SerializedObject serializedClip = new SerializedObject(materialAdjustAnim);
                AnimationClipSettings clipSettings = new AnimationClipSettings
                    (serializedClip.FindProperty("m_AnimationClipSettings"));
                clipSettings.loopTime = false;
                serializedClip.ApplyModifiedProperties();

                Debug.Log("Attempting To Create Animation For: " + animatableProperty.animationName);
                string[] res = System.IO.Directory.GetFiles(Application.dataPath, "GenerateMatPropertyAnims.cs", 
                    SearchOption.AllDirectories);
                string path = res[0].Replace("GenerateMatPropertyAnims.cs", "").Replace("\\", "/");
                string[] splitPath = path.Split(new string[] {"Assets"}, System.StringSplitOptions.None);
                path = "Assets" + splitPath[1] + ("Animations/" + animatableProperty.animationName + "_" + DateTime.Now.ToString("ddHHmmss") + ".anim");
                
                Debug.Log("Creating Animation At Path: " + path);
                AssetDatabase.CreateAsset(materialAdjustAnim, path);
            }

            catch (Exception e)
            {
                Debug.LogError("Error Generating Animation For ("  + materialAdjustAnim + "): " + e);
            }

        }

        private void RegisterAnimatableProperties()
        {
            propertiesRegistered = true;

            aprop_monochromaticMultiplier = new AnimatableProperty
            {
                minValue = 0f,
                midValue = 0.5f,
                maxValue = 1.0f,
                propertyName = "_LightingMonochromatic",
                animationName = "Monochromatic Control"
            };

            aprop_monochromaticAdditiveMultiplier = new AnimatableProperty
            {
                minValue = 0f,
                midValue = 0.5f,
                maxValue = 1.0f,
                propertyName = "_LightingAdditiveMonochromatic",
                animationName = "Monochromatic Additive Control"
            };

            aprop_lightingMultiplier = new AnimatableProperty
            {
                minValue = 0f,
                midValue = 1.35f,
                maxValue = 3f,
                propertyName = "_PPLightingMultiplier",
                animationName = "Lighting Control"
            };

            aprop_emisisonMultiplier = new AnimatableProperty
            {
                minValue = 0f,
                midValue = 1f,
                maxValue = 4f,
                propertyName = "_PPEmissionMultiplier",
                animationName = "Emission Control"
            };
        }

        void OnGUI()
        {
            string infoMsg =
                "This tool will auto generate an animate with the desired settings\n\n" +
                "#[Material Property Name] - The Shader Property That Will Be Animated" +
                "#[Min Value] - Animation Start Value\n" +
                "#[Mid Value] - Animation Mid Value\n" +
                "#[Max Value] - Animation Max Value\n" +
                "#[Animatable Suffix] - Suffix Used To Mark Properties As Animatable In Shaders Like Poiyomi ('Animated' in Poi v8)\n" +
                "#[Shader Tag] - Brief Shader Name So That Only Those Shaders Get Animated (e.g .poiyomi)";

            EditorGUILayout.HelpBox (infoMsg, MessageType.Warning);
            EditorGUILayout.Space(10);
            
            if(!propertiesRegistered) RegisterAnimatableProperties();
            
            materialAnimationName = EditorGUILayout.TextField("Animation Name: ", materialAnimationName);
            avatar = (GameObject)EditorGUILayout.ObjectField("Avatar: ", avatar, typeof(GameObject), true);
            GUILayout.Space(15);
            
            materialPropertyName = EditorGUILayout.TextField("Material Property Name: ", materialPropertyName);
            propertyMinValue = EditorGUILayout.FloatField("Min Value: ", propertyMinValue);
            propertyMidValue = EditorGUILayout.FloatField("Mid Value: ", propertyMidValue);
            propertyMaxValue = EditorGUILayout.FloatField("Max Value: ", propertyMaxValue);
            
            GUILayout.Space(15);
            animatableSuffix = EditorGUILayout.TextField("Animatable Suffix: ", animatableSuffix);
            shaderTag = EditorGUILayout.TextField("Shader Tag: ", shaderTag);
            
            if (avatar != null && GUILayout.Button("Generate Animations!") && !finished)
            {
                materialAdjustAnim = new AnimationClip();
                
                AnimatableProperty customProperty = new AnimatableProperty()
                {
                    minValue = propertyMinValue,
                    midValue = propertyMidValue,
                    maxValue = propertyMaxValue,
                    propertyName = materialPropertyName,
                    animationName = materialAnimationName
                };
                
                GenerateAnims(customProperty);
                
                finished = true;
            }

            if (avatar != null && GUILayout.Button("Generate Poi Lighting Anim") && !finished)
            {
                materialAdjustAnim = new AnimationClip();
                
                GenerateAnims(aprop_lightingMultiplier);
                
                finished = true;
            }
            
            if (avatar != null && GUILayout.Button("Generate Poi Monochromatic Anim") && !finished)
            {
                materialAdjustAnim = new AnimationClip();
                
                GenerateAnims(aprop_monochromaticMultiplier);
                
                finished = true;
            }
            
            if (avatar != null && GUILayout.Button("Generate Poi Emission Anim") && !finished)
            {
                materialAdjustAnim = new AnimationClip();
                
                GenerateAnims(aprop_emisisonMultiplier);
                
                finished = true;
            }

            if (finished)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Done! :)", MessageType.Info);
                if(GUILayout.Button("Continue!"))
                {
                    materialAdjustAnim = null;
                    renderersAnimated = new List<Renderer>();
                    finished = false;
                }
            }

        }
    }
}
#endif