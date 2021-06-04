using UnityEngine;
using UnityEditor;

namespace ABS
{
    public class ModelEditor : Editor
    {
        protected bool DrawGroundPivotField(Model model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isGroundPivot = EditorGUILayout.Toggle(new GUIContent("Ground Pivot",
                "includes vertical range from ground to bottom of this object"), model.isGroundPivot);
            isChanged = EditorGUI.EndChangeCheck();

            return isGroundPivot;
        }

        protected GameObject DrawSpritePrefabField(Model model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            GameObject spritePrefab = EditorGUILayout.ObjectField(new GUIContent("Sprite Prefab",
                "output sprite object to instantiate"), model.spritePrefab, typeof(GameObject), false) as GameObject;
            isChanged = EditorGUI.EndChangeCheck();

            return spritePrefab;
        }

        protected PrefabBuilder DrawPrefabBuilderField(Model model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            PrefabBuilder prefabBuilder = EditorGUILayout.ObjectField(new GUIContent("Prefab Builder",
                    "helps to construct output sprite object"), model.prefabBuilder, typeof(PrefabBuilder), false) as PrefabBuilder;
            isChanged = EditorGUI.EndChangeCheck();

            return prefabBuilder;
        }

        protected string DrawModelNameSuffix(Model model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            string nameSuffix = EditorGUILayout.TextField(new GUIContent("Name Suffix",
                "concatenates after model name"), model.nameSuffix);
            isChanged = EditorGUI.EndChangeCheck();

            return nameSuffix;
        }

        protected void AddToModelList(Model model)
        {
            Studio studio = FindObjectOfType<Studio>();
            if (studio == null)
                return;

            studio.AddModel(model);
        }
    }
}
