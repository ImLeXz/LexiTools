#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using Lexi.Extensions;

namespace Lexi.HelperFunctions
{
    public static class Helpers
    {
        public static string GetPathNoName(string path)
        {
            if(StringIsNullOrWhiteSpace(path))
                return path;

            string reverse = new string(path.ToArray().Reverse().ToArray());
            char[] slashes = new char[] { '/', '\\' };
            int firstSlash = reverse.IndexOfAny(slashes);

            if (firstSlash == 0)
            {
                if (firstSlash + 1 < reverse.Length)
                    firstSlash = reverse.IndexOfAny(slashes, firstSlash + 1);
                else
                    return "";
            }

            if (firstSlash == -1)
                return "";


            reverse = reverse.Substring(firstSlash);
            string s = new string(reverse.ToArray().Reverse().ToArray());
            return s;
        }
        
        public static bool StringIsNullOrWhiteSpace(string value)
        {
            if (value != null)
            {
                for (int i = 0; i < value.Length; i++)
                {
                    if (!char.IsWhiteSpace(value[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static string[] GetPathAsArray(string path)
        {
            if(string.IsNullOrEmpty(path))
                return null;

            return path.Split('\\', '/');
        }
        
        public static Transform FindTransformInAnotherHierarchy(Transform trans, Transform otherHierarchyTrans, bool createIfMissing)
        {
            if(!trans || !otherHierarchyTrans)
                return null;

            if(trans == trans.root)
                return otherHierarchyTrans.root;

            var childPath = GetGameObjectPath(trans);
            var childTrans = otherHierarchyTrans.Find(childPath, createIfMissing, trans);

            return childTrans;
        }
        
        public static bool GameObjectIsEmpty(GameObject obj)
        {
            if(obj.GetComponentsInChildren<Component>().Length > obj.GetComponentsInChildren<Transform>().Length)
                return false;
            return true;
        }
        
        public static string GetGameObjectPath(GameObject obj, bool skipRoot = true)
        {
            if(!obj)
                return string.Empty;

            string path = string.Empty;
            if(obj.transform != obj.transform.root)
            {
                if(!skipRoot)
                    path = obj.transform.root.name + "/";
                path += (AnimationUtility.CalculateTransformPath(obj.transform, obj.transform.root));
            }
            else
            {
                if(!skipRoot)
                    path = obj.transform.root.name;
            }
            return path;
        }
        
        public static string GetGameObjectPath(Transform trans, bool skipRoot = true)
        {
            if(trans != null)
                return GetGameObjectPath(trans.gameObject, skipRoot);
            return null;
        }
        
        public static Transform RecurseFind(Transform obj, string name)
        {

            Transform find = obj.Find(name);

            if (find)
            {
                return find;
            }

            for (int i = 0; i < obj.childCount; i++)
            {
                find = RecurseFind(obj.GetChild(i), name);
                if (find)
                {
                    return find;
                }
            }

            return null;

        }
        
    }
}
#endif
