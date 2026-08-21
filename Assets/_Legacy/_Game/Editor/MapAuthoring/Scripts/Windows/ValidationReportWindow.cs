#if LEGACY_DISABLED
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Editor
{
    public sealed class ValidationReportWindow : EditorWindow
    {
        private MapElementValidationReport report;
        private Vector2 scroll;

        public static void ShowReport(MapElementValidationReport validationReport)
        {
            var window = GetWindow<ValidationReportWindow>();
            window.titleContent = new GUIContent("Map Validation");
            window.minSize = new Vector2(620f, 360f);
            window.report = validationReport;
            window.Show();
            window.Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("MAP-E04 · Validation Report", EditorStyles.boldLabel);
            if (report == null)
            {
                EditorGUILayout.HelpBox("검증 결과가 없습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                report.CreateSummary(),
                report.IsValid ? MessageType.Info : MessageType.Error);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (var index = 0; index < report.Issues.Count; index++)
            {
                var issue = report.Issues[index];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var previousColor = GUI.color;
                    GUI.color = issue.Severity == ValidationSeverity.Error
                        ? new Color(1f, 0.55f, 0.5f)
                        : new Color(1f, 0.85f, 0.45f);
                    EditorGUILayout.LabelField(
                        $"{issue.Severity} · {issue.Code}",
                        EditorStyles.boldLabel);
                    GUI.color = previousColor;
                    EditorGUILayout.LabelField(issue.Message, EditorStyles.wordWrappedLabel);
                    if (!string.IsNullOrWhiteSpace(issue.AssetPath))
                    {
                        EditorGUILayout.LabelField(issue.AssetPath, EditorStyles.miniLabel);
                    }
                    if (issue.AutoFixable)
                    {
                        EditorGUILayout.LabelField("자동 수정 허용", EditorStyles.miniBoldLabel);
                    }
                    if (issue.Context != null && GUILayout.Button("대상 선택", GUILayout.Width(90f)))
                    {
                        Selection.activeObject = issue.Context;
                        EditorGUIUtility.PingObject(issue.Context);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
}

#endif
