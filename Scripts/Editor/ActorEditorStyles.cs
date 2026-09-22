using UnityEditor;
using UnityEngine;

namespace Abc.Unity.Editor
{
    internal static class ActorEditorStyles
    {
        private static GUIStyle _title;
        private static GUIStyle _subtitle;
        private static GUIStyle _section;
        private static GUIStyle _metricValue;
        private static GUIStyle _metricLabel;
        private static GUIStyle _centeredButton;

        public static Color Accent => EditorGUIUtility.isProSkin
            ? new Color(0.29f, 0.72f, 1f)
            : new Color(0.05f, 0.42f, 0.72f);

        public static Color DataColor => EditorGUIUtility.isProSkin
            ? new Color(0.22f, 0.55f, 0.75f, 0.18f)
            : new Color(0.15f, 0.55f, 0.82f, 0.12f);

        public static Color BehaviourColor => EditorGUIUtility.isProSkin
            ? new Color(0.68f, 0.42f, 0.88f, 0.18f)
            : new Color(0.55f, 0.25f, 0.72f, 0.12f);

        public static GUIStyle Section => _section ??= new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12,
            margin = new RectOffset(0, 0, 2, 4)
        };

        public static GUIStyle CenteredButton => _centeredButton ??= new GUIStyle(GUI.skin.button)
        {
            fixedHeight = 28f,
            fontStyle = FontStyle.Bold
        };

        public static void DrawHeader(string title, string subtitle, string iconName)
        {
            EnsureHeaderStyles();
            var rect = GUILayoutUtility.GetRect(0f, 68f, GUILayout.ExpandWidth(true));
            var background = EditorGUIUtility.isProSkin
                ? new Color(0.105f, 0.12f, 0.145f)
                : new Color(0.91f, 0.94f, 0.97f);
            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4f, rect.height), Accent);

            var icon = EditorGUIUtility.IconContent(iconName).image;
            if (icon != null)
                GUI.DrawTexture(new Rect(rect.x + 16f, rect.y + 14f, 40f, 40f), icon, ScaleMode.ScaleToFit);

            var textX = rect.x + 68f;
            GUI.Label(new Rect(textX, rect.y + 11f, rect.width - 80f, 25f), title, _title);
            GUI.Label(new Rect(textX, rect.y + 36f, rect.width - 80f, 20f), subtitle, _subtitle);
            GUILayout.Space(7f);
        }

        public static void BeginCard()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        }

        public static void EndCard()
        {
            EditorGUILayout.EndVertical();
            GUILayout.Space(3f);
        }

        public static void DrawMetric(string value, string label)
        {
            EnsureMetricStyles();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(48f));
            GUILayout.Label(value, _metricValue);
            GUILayout.Label(label, _metricLabel);
            EditorGUILayout.EndVertical();
        }

        public static void DrawDivider()
        {
            var rect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.24f));
        }

        private static void EnsureHeaderStyles()
        {
            _title ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleLeft
            };
            _subtitle ??= new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft
            };
        }

        private static void EnsureMetricStyles()
        {
            _metricValue ??= new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            _metricLabel ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
