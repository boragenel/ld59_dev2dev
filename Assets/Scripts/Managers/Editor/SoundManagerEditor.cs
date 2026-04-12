using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System;

namespace com.brainoid.skull
{

    [CustomEditor(typeof(SoundManager), true)]
    public class SoundManagerEditor : Editor
    {
        SoundManager myTarget;

        GUIStyle HeaderStyle;

        private void Awake()
        {
            myTarget = (SoundManager)target;


        }



        public override void OnInspectorGUI()
        {
           
            DrawDefaultInspector();

            
            if (GUILayout.Button("Create Sound Enums"))
            {
                CreateEnums();
            }



        }


        private void CreateEnums()
        {
            string classDef = string.Empty;
            classDef += "public enum SoundType" + Environment.NewLine;
            classDef += "{" + Environment.NewLine;
            string enumName;
            for (int i=0; i < myTarget.sounds.Length; i++ )
            {
               enumName = myTarget.sounds[i].name.ToUpperInvariant().Replace(' ', '_');
                classDef += " " + enumName +","+ Environment.NewLine;
            }
            classDef += "}" + Environment.NewLine;
            string path = "Assets/Scripts/Types/SoundType.cs";
            FileStream fs = null;
            if (!File.Exists(path))
            {
                fs = File.Create(path);
            }
            File.WriteAllText(path, classDef);
            Debug.Log("ENUMS CREATED");
        }
    }


}


