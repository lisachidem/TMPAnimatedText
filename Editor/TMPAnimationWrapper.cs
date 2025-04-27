#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace TMPAnimatedEffects.Editor
{
    [CustomEditor(typeof(TMPAnimatedEffects))]
    public class TMPAnimatedEffectsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var effects = (TMPAnimatedEffects)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Animation Codes", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("TypeWriter", "[typewriter speed=10] Your Text [/typewriter]");
            EditorGUILayout.TextField("Wave", "[wave amp=6 freq=4] Your Text [/wave]");
            EditorGUILayout.TextField("Shake", "[shake amt=2] Your Text [/shake]");
            EditorGUILayout.TextField("Glitch", "[glitch intensity=3] Your Text [/glitch]");
            EditorGUILayout.TextField("Rainbow", "[rainbow speed=2] Your Text [/rainbow]");
            EditorGUILayout.TextField("Fade", "[fade duration=3 endAlpha=1] Your Text [/fade]");
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            if (!Application.isPlaying)
            {
                return;
            }

            if (GUILayout.Button("Replay Effects"))
            {
                effects.SendMessage("Initialize");
            }
        }
    }
}

#endif