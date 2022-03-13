#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Lexi
{
    public class SetMaterialProperty : ScriptableWizard
    {
        public enum PropertyType
        {
            Color,
            Int,
            Float,
            Texture
        };
        
        [FormerlySerializedAs("avatar")] public GameObject parentObj;
        public string materialPropertyName;
        public PropertyType propertyType;
        public string shaderTag = ".poiyomi";
        
        private Color colorProp = Color.white;
        private Texture2D textureProp;
        private int intProp = 0;
        private float floatProp = 0.0f;
        
        private Color oldColorProp = Color.white;
        private Texture2D oldTextureProp;
        private int oldIntProp = 0;
        private float oldFloatProp = 0.0f;

        bool finished;


        public SetMaterialProperty()
        {
            finished = false;
        }

        [MenuItem("Lexi/Set Material Properties")]
        static void CreateWizard()
        {
            SetMaterialProperty wiz = ScriptableWizard.DisplayWizard<SetMaterialProperty>("Set Material Properties");
        }


        Material[] GetProcessMaterials()
        {
            List<Material> materialsToProcess = new List<Material>();
            Renderer[] meshes = parentObj.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < (int) meshes.Length; i++)
            {
                Type meshType = meshes[i].GetType();
                if (meshType == typeof(MeshRenderer) || meshType == typeof(SkinnedMeshRenderer))
                {
                    bool shouldProcessMesh = true;
                    if (!String.IsNullOrEmpty(shaderTag))
                    {
                        foreach (var VARIABLE in meshes[i].sharedMaterials)
                        {
                            if (!VARIABLE.shader.name.Contains(shaderTag)) shouldProcessMesh = false;
                            if (shouldProcessMesh)
                            {
                                Debug.Log("Processing Material: " + VARIABLE);
                                if (VARIABLE.HasProperty(materialPropertyName))
                                {
                                    materialsToProcess.Add(VARIABLE);
                                }
                                else
                                {
                                    Debug.LogError("MATERIAL [" + VARIABLE.name + "] DOES NOT HAVE " +
                                                   "PROPERTY NAMED: " + materialPropertyName);
                                }
                            }
                        }
                    }

                    if (!shouldProcessMesh)
                    {
                        Debug.LogWarning("Couldn't Find Any Materials With Shader Tag: " + shaderTag);
                        continue;
                    }


                }
            }


            return materialsToProcess.ToArray();
        }

        private void RevertProperties(Material mat)
        {
            Debug.Log("Reverting Property On: " + mat.name);
            switch (propertyType)
            {
                case PropertyType.Color:
                    mat.SetColor(materialPropertyName, oldColorProp);
                    break;
                case PropertyType.Int:
                    mat.SetInt(materialPropertyName, oldIntProp);
                    break;
                case PropertyType.Float:
                    mat.SetFloat(materialPropertyName, oldFloatProp);
                    break;
                case PropertyType.Texture:
                    mat.SetTexture(materialPropertyName, oldTextureProp);
                    break;
            }
        }
        
        private void SetProperty(Material mat)
        {
            Debug.Log("Setting Property On: " + mat.name);
            switch (propertyType)
            {
                case PropertyType.Color:
                    oldColorProp = mat.GetColor(materialPropertyName);
                    mat.SetColor(materialPropertyName, colorProp);
                    break;
                case PropertyType.Int:
                    oldIntProp = mat.GetInt(materialPropertyName);
                    mat.SetInt(materialPropertyName, intProp);
                    break;
                case PropertyType.Float:
                    oldFloatProp = mat.GetFloat(materialPropertyName);
                    mat.SetFloat(materialPropertyName, floatProp);
                    break;
                case PropertyType.Texture:
                    oldTextureProp = (Texture2D)mat.GetTexture(materialPropertyName);
                    mat.SetTexture(materialPropertyName, textureProp);
                    break;
            }
        }
        
        private static Texture2D TextureField(string name, Texture2D texture)
        {
            GUILayout.BeginVertical();
            var style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.UpperCenter;
            style.fixedWidth = 70;
            GUILayout.Label(name, style);
            var result = (Texture2D)EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width(70), GUILayout.Height(70));
            GUILayout.EndVertical();
            return result;
        }

        void OnGUI()
        {
            string infoMsg =
                "This tool will auto generate an animate with the desired settings\n\n" +
                "#[Material Property Name] - The Shader Property That Will Be Changed\n" +
                "#[Property Type] - The Type Of The Shader Property\n" +
                "#[Shader Tag] - Brief Shader Name So That Only Those Shaders Get Changed (e.g .poiyomi)";

            string warningMsg = "THIS TOOL CAN ONLY SET ALL MATERIALS TO A SINGLE PROPERTY, " +
                                "SO IF YOUR MATERIALS HAVE MULTIPLE UNIQUE PROPERTIES IT WILL NOT REVERT " +
                                "THEM PROPERLY";

            EditorGUILayout.HelpBox (infoMsg, MessageType.Warning);
            EditorGUILayout.HelpBox (warningMsg, MessageType.Warning);
            EditorGUILayout.Space(10);
            
            parentObj = (GameObject)EditorGUILayout.ObjectField("Parent Obj: ", parentObj, typeof(GameObject), true);
            GUILayout.Space(15);
            
            materialPropertyName = EditorGUILayout.TextField("Material Property Name: ", materialPropertyName);
            propertyType = (PropertyType)EditorGUILayout.EnumPopup(propertyType);
            
            GUILayout.Space(2);
            switch (propertyType)
            {
                default:
                    break;
                case PropertyType.Color:
                    colorProp = EditorGUILayout.ColorField("New Color Value: ", colorProp);
                    break;
                case PropertyType.Int:
                    intProp = EditorGUILayout.IntField("New Int Value: ", intProp);
                    break;
                case PropertyType.Float:
                    floatProp = EditorGUILayout.FloatField("New Float Value: ", floatProp);
                    break;
                case PropertyType.Texture:
                    textureProp = TextureField("New Texture Value: ", textureProp);
                    break;
            }

            GUILayout.Space(15);
            shaderTag = EditorGUILayout.TextField("Shader Tag: ", shaderTag);

            if (!String.IsNullOrEmpty(materialPropertyName) && parentObj != null &&
                GUILayout.Button("Set Material Properties") && !finished)
            {
                Material[] materials = GetProcessMaterials();
                foreach (var VARIABLE in materials)
                {
                    SetProperty(VARIABLE);
                }
                finished = true;
            }

            if (finished)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Done! :)", MessageType.Info);
                if(GUILayout.Button("Revert Changes"))
                {
                    Material[] materials = GetProcessMaterials();
                    foreach (var VARIABLE in materials)
                    {
                        RevertProperties(VARIABLE);
                    }
                    finished = false;
                }
                EditorGUILayout.Space(2);
                if(GUILayout.Button("Continue!"))
                {
                    finished = false;
                }
            }

        }
    }
}
#endif