using UnityEngine;
using UnityEditor;

namespace ABS
{
    [CustomEditor(typeof(ParticleModel)), CanEditMultipleObjects]
    public class ParticleModelEditor : ModelEditor
    {
        private ParticleModel model = null;

        void OnEnable()
        {
            model = target as ParticleModel;
        }

        public override void OnInspectorGUI()
        {
            GUI.changed = false;

            if (targets != null && targets.Length > 1)
                OnInspectorGUI_Multi();
            else if (model != null)
                OnInspectorGUI_Single();
        }

        private void OnInspectorGUI_Single()
        {
            Undo.RecordObject(model, "Particle Model");

            bool isAnyChanged = false;

            EditorGUI.BeginChangeCheck();
            model.mainParticleSystem = EditorGUILayout.ObjectField("Main Particle System",
                model.mainParticleSystem, typeof(ParticleSystem), true) as ParticleSystem;
            if (EditorGUI.EndChangeCheck())
                model.targetChecked = false;

            if (model.mainParticleSystem == null)
                model.TrySetMainParticleSystem();

            if (model.mainParticleSystem != null)
            {
                if (!model.targetChecked)
                    model.CheckModel();

                if (model.animationClip != null)
                {
                    EditorGUI.indentLevel++;
                    GUI.enabled = false;
                    EditorGUILayout.FloatField("Duration", model.mainParticleSystem.main.duration, EditorStyles.label);
                    GUI.enabled = true;
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();

            model.isGroundPivot = DrawGroundPivotField(model, out isAnyChanged);

            EditorGUILayout.Space();

            model.isLooping = DrawLoopingField(model, out isAnyChanged);
            if (model.isLooping)
            {
                EditorGUI.indentLevel++;
                model.isPrewarm = DrawPrewarmField(model, out isAnyChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            model.spritePrefab = DrawSpritePrefabField(model, out isAnyChanged);
            if (model.spritePrefab != null)
            {
                EditorGUI.indentLevel++;
                model.prefabBuilder = DrawPrefabBuilderField(model, out isAnyChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool isNameSuffixChanged;
            model.nameSuffix = DrawModelNameSuffix(model, out isNameSuffixChanged);
            if (isNameSuffixChanged)
                PathHelper.CorrectPathString(ref model.nameSuffix);

            EditorGUILayout.Space();

            if (DrawingHelper.DrawWideButton("Add to the model list"))
                AddToModelList(model);
        }

        protected void OnInspectorGUI_Multi()
        {
            EditorGUILayout.HelpBox("Displayed information is of the first selected model,\nbut any change affects all selected models.", MessageType.Info);

            ParticleModel[] models = new ParticleModel[targets.Length];

            for (int i = 0; i < models.Length; ++i)
                models[i] = targets[i] as ParticleModel;

            ParticleModel firstModel = models[0];

            EditorGUILayout.Space();

            bool isGroundPivotChanged;
            bool isGroundPivot = DrawGroundPivotField(firstModel, out isGroundPivotChanged);

            EditorGUILayout.Space();

            bool isLoopingChanged;
            bool isLooping = DrawLoopingField(firstModel, out isLoopingChanged);

            bool isAllLooping = true;
            foreach (ParticleModel model in models)
                isAllLooping &= model.isLooping;

            bool isPrewarmChanged = false;
            bool isPrewarm = false;
            if (isAllLooping)
            {
                EditorGUI.indentLevel++;
                isPrewarm = DrawPrewarmField(firstModel, out isPrewarmChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool isSpritePrefabChanged;
            GameObject spritePrefab = DrawSpritePrefabField(firstModel, out isSpritePrefabChanged);

            bool hasAllSpritePrefab = true;
            foreach (ParticleModel model in models)
                hasAllSpritePrefab &= (model.spritePrefab != null);

            PrefabBuilder prefabBuilder = null;
            bool isPrefabBuilderChanged = false;
            if (hasAllSpritePrefab)
            {
                EditorGUI.indentLevel++;
                prefabBuilder = DrawPrefabBuilderField(model, out isPrefabBuilderChanged);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            bool isNameSuffixChanged;
            string nameSuffix = DrawModelNameSuffix(firstModel, out isNameSuffixChanged);
            if (isNameSuffixChanged)
                PathHelper.CorrectPathString(ref nameSuffix);

            if (isGroundPivotChanged || isLoopingChanged || isPrewarmChanged ||
                isSpritePrefabChanged || isPrefabBuilderChanged || isNameSuffixChanged)
            {
                foreach (ParticleModel model in models)
                {
                    Undo.RecordObject(model, "Particle Model");
                    if (isGroundPivotChanged)
                        model.isGroundPivot = isGroundPivot;
                    if (isLoopingChanged)
                        model.isLooping = isLooping;
                    if (isPrewarmChanged)
                        model.isPrewarm = isPrewarm;
                    if (isSpritePrefabChanged)
                        model.spritePrefab = spritePrefab;
                    if (hasAllSpritePrefab && isPrefabBuilderChanged)
                        model.prefabBuilder = prefabBuilder;
                    if (isNameSuffixChanged)
                        model.nameSuffix = nameSuffix;
                }
            }

            Studio studio = FindObjectOfType<Studio>();
            if (studio == null)
                return;

            EditorGUILayout.Space();

            if (DrawingHelper.DrawWideButton("Add all to the model list"))
            {
                foreach (ParticleModel model in models)
                    AddToModelList(model);
            }
        }

        private void DrawAnimationFields(ParticleModel model, out AnimationClip animationClip, out float animStartTime, out bool isCameraFollowing)
        {
            animStartTime = model.animStartTime;
            isCameraFollowing = model.isCameraFollowing;

            animationClip = EditorGUILayout.ObjectField(new GUIContent("Animation",
                "additional animation that transforms this object's rotation or position"),
                model.animationClip, typeof(AnimationClip), false) as AnimationClip;

            if (animationClip != null && model.mainParticleSystem != null)
            {
                EditorGUI.indentLevel++;

                GUI.enabled = false;
                EditorGUILayout.FloatField("Length", model.animationClip.length, EditorStyles.label);
                GUI.enabled = true;

                float particleLength = model.mainParticleSystem.main.duration;

                if (animationClip.length > particleLength)
                    EditorGUILayout.HelpBox("Animation's length is longer than Main Particle System's duration.", MessageType.Warning);

                EditorGUI.BeginChangeCheck();
                animStartTime = EditorGUILayout.FloatField(new GUIContent("Start Time",
                    "animation's start time in the main particle system's duration"), model.animStartTime);
                if (EditorGUI.EndChangeCheck())
                {
                    if (animStartTime < 0)
                        animStartTime = 0;
                    else if (animStartTime + animationClip.length > particleLength)
                        animStartTime = particleLength - animationClip.length;
                }

                float animEndTime = animStartTime + animationClip.length;

                GUI.enabled = false;
                EditorGUILayout.MinMaxSlider(new GUIContent("Play Range",
                    "animation's play time range in the main particle system's duration"), ref animStartTime, ref animEndTime, 0, particleLength);
                GUI.enabled = true;

                isCameraFollowing = EditorGUILayout.Toggle(new GUIContent("Camera Following",
                    "makes main camera follow this object at every frame"), model.isCameraFollowing);

                EditorGUI.indentLevel--;
            }   
        }

        private bool DrawLoopingField(ParticleModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isLooping = EditorGUILayout.Toggle(new GUIContent("Looping", "generates looping animation clip"), model.isLooping);
            isChanged = EditorGUI.EndChangeCheck();

            return isLooping;
        }

        private bool DrawPrewarmField(ParticleModel model, out bool isChanged)
        {
            EditorGUI.BeginChangeCheck();
            bool isPrewarm = EditorGUILayout.Toggle(new GUIContent("Prewarm",
                "simulates for the final frame at first instead of the zero frame"), model.isPrewarm);
            isChanged = EditorGUI.EndChangeCheck();

            return isPrewarm;
        }
    }
}
