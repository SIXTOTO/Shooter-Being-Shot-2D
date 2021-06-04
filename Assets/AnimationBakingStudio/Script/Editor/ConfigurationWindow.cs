using UnityEditor;

namespace ABS
{
    public class ConfigurationWindow : EditorWindow
    {
        public const string GPU_USE_KEY = "GpuUse";

        [MenuItem("Assets/" + EngineGlobal.PROJECT_NAME + "/Configuration")]
        public static void Init()
        {
            ConfigurationWindow window = GetWindow<ConfigurationWindow>("Configuration");
            window.Show();
        }

        void OnEnable()
        {
            UpdateGlobalVariables();   
        }

        public static void UpdateGlobalVariables()
        {
            EngineGlobal.gpuUse = EditorPrefs.GetBool(GPU_USE_KEY, false);
        }

        void OnGUI()
        {
            Studio studio = FindObjectOfType<Studio>();
            if (studio == null)
            {
                EditorGUILayout.HelpBox("No Studio object", MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EngineGlobal.gpuUse = EditorGUILayout.Toggle("Use GPU", EngineGlobal.gpuUse);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(GPU_USE_KEY, EngineGlobal.gpuUse);
        }
    }
}
