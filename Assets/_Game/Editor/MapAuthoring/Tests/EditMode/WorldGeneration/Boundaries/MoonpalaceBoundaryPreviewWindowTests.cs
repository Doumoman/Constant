using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Map.WorldGeneration.Boundaries;
using StarNight.MapAuthoring.Boundaries;
using UnityEditor;
using UnityEngine;

namespace StarNight.MapAuthoring.Tests.WorldGeneration.Boundaries
{
    [Category("MAP08_13")]
    public sealed class MoonpalaceBoundaryPreviewWindowTests
    {
        private const string ExpectedDigest =
            "f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68";

        public static IEnumerable<TestCaseData> ContractCases
        {
            get
            {
                for (var caseId = 0; caseId < 220; caseId++)
                {
                    yield return new TestCaseData(caseId)
                        .SetName("MoonpalaceBoundaryPreviewWindowContract_" + caseId.ToString("D3"));
                }
            }
        }

        [TestCaseSource(nameof(ContractCases))]
        public void MoonpalaceBoundaryPreviewWindowContract(int caseId)
        {
            var variant = caseId / 11;
            switch (caseId % 11)
            {
                case 0: AssertMenuPath(); break;
                case 1: AssertMenuCommandRegistered(); break;
                case 2: AssertWindowUsesApprovedViewModel(); break;
                case 3: AssertDigestCopyIsClipboardOnly(); break;
                case 4: AssertSummaryCopyIsClipboardOnly(); break;
                case 5: AssertCandidateSelectionCommand(variant); break;
                case 6: AssertOverlayCommand(variant); break;
                case 7: AssertRefreshCommand(variant); break;
                case 8: AssertNoSceneObjectDependency(); break;
                case 9: AssertOpenCommand(variant); break;
                case 10: AssertInvalidCandidateIsNonSelecting(); break;
            }
        }

        private static MoonpalaceBoundaryPreviewViewModel Approved()
        {
            var value = MoonpalaceBoundaryPreviewViewModel.LoadApprovedAuthoring();
            Assert.That(value.Accepted, Is.True,
                string.Join("\n", value.CurrentReport.Issues.Select(issue => issue.ToString())));
            return value;
        }

        private static MoonpalaceBoundaryPreviewWindow CreateWindow(
            MoonpalaceBoundaryPreviewViewModel value = null)
        {
            var window = ScriptableObject.CreateInstance<MoonpalaceBoundaryPreviewWindow>();
            window.UseViewModel(value ?? Approved());
            return window;
        }

        private static void AssertMenuPath()
        {
            Assert.That(
                MoonpalaceBoundaryPreviewWindow.MenuPath,
                Is.EqualTo("Tools/Map/Moonpalace Boundary Preview"));
        }

        private static void AssertMenuCommandRegistered()
        {
            var method = typeof(MoonpalaceBoundaryPreviewWindow).GetMethod(
                nameof(MoonpalaceBoundaryPreviewWindow.Open),
                BindingFlags.Public | BindingFlags.Static);
            var attribute = (MenuItem)Attribute.GetCustomAttribute(method, typeof(MenuItem));
            Assert.That(method, Is.Not.Null);
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.menuItem, Is.EqualTo(MoonpalaceBoundaryPreviewWindow.MenuPath));
        }

        private static void AssertWindowUsesApprovedViewModel()
        {
            var window = CreateWindow();
            try
            {
                Assert.That(window.ViewModel, Is.Not.Null);
                Assert.That(window.LastReport.Accepted, Is.True);
                Assert.That(window.LastReport.PairRows.Count, Is.EqualTo(6));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static void AssertDigestCopyIsClipboardOnly()
        {
            var previous = EditorGUIUtility.systemCopyBuffer;
            var window = CreateWindow();
            try
            {
                Assert.That(window.CopyStableDigest(), Is.True);
                Assert.That(EditorGUIUtility.systemCopyBuffer, Is.EqualTo(ExpectedDigest));
                Assert.That(window.LastReport.CoverageReport.GeneratedCsvCount, Is.Zero);
            }
            finally
            {
                EditorGUIUtility.systemCopyBuffer = previous;
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static void AssertSummaryCopyIsClipboardOnly()
        {
            var previous = EditorGUIUtility.systemCopyBuffer;
            var window = CreateWindow();
            try
            {
                Assert.That(window.CopyReportSummary(), Is.True);
                Assert.That(EditorGUIUtility.systemCopyBuffer, Is.EqualTo(window.LastReport.Summary));
                Assert.That(EditorGUIUtility.systemCopyBuffer, Does.Contain("31/31/2976/62"));
            }
            finally
            {
                EditorGUIUtility.systemCopyBuffer = previous;
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static void AssertCandidateSelectionCommand(int variant)
        {
            var value = Approved();
            var pair = value.CoverageReport.PairReports[variant % 6];
            value.SelectPair(pair.PairRuleId);
            value.SelectProfile(pair.Requirement.DefaultProfileId);
            value.SelectOrientation(variant % 2 == 0
                ? MoonpalaceBoundaryPreviewSelection.HorizontalToken
                : MoonpalaceBoundaryPreviewSelection.VerticalToken);
            var index = value.CurrentReport.SelectedCandidate.SourceIndex;
            var window = CreateWindow(value);
            try
            {
                Assert.That(window.TrySelectCandidate(index), Is.True);
                Assert.That(window.LastReport.Cells.Count, Is.EqualTo(96));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static void AssertOverlayCommand(int variant)
        {
            var value = Approved();
            var toggle = variant % 2 == 0
                ? MoonpalaceBoundaryPreviewOverlayToggle.Foreground
                : MoonpalaceBoundaryPreviewOverlayToggle.Background;
            var window = CreateWindow(value);
            try
            {
                value.SetOverlay(toggle, false);
                Assert.That((window.LastReport.Overlays & toggle) == toggle, Is.False);
                Assert.That(window.LastReport.Cells.All(cell =>
                    toggle == MoonpalaceBoundaryPreviewOverlayToggle.Foreground
                        ? !cell.ShowForeground
                        : !cell.ShowBackground), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static void AssertRefreshCommand(int variant)
        {
            if (variant != 0)
            {
                Assert.That(typeof(MoonpalaceBoundaryPreviewWindow).GetMethod(
                    nameof(MoonpalaceBoundaryPreviewWindow.RefreshFromAuthoring)), Is.Not.Null);
                return;
            }
            var window = CreateWindow();
            try
            {
                Assert.That(window.RefreshFromAuthoring(), Is.True);
                Assert.That(window.LastReport.StableDigest, Is.EqualTo(ExpectedDigest));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static void AssertNoSceneObjectDependency()
        {
            Assert.That(typeof(MoonpalaceBoundaryPreviewWindow).IsSubclassOf(typeof(EditorWindow)), Is.True);
            Assert.That(typeof(MoonpalaceBoundaryPreviewWindow)
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Any(field => typeof(GameObject).IsAssignableFrom(field.FieldType)), Is.False);
        }

        private static void AssertOpenCommand(int variant)
        {
            if (variant != 0)
            {
                Assert.That(typeof(MoonpalaceBoundaryPreviewWindow).IsSealed, Is.True);
                return;
            }
            var window = MoonpalaceBoundaryPreviewWindow.Open();
            try
            {
                Assert.That(window.LastReport, Is.Not.Null);
                Assert.That(window.titleContent.text, Is.EqualTo("Boundary Preview"));
            }
            finally
            {
                window.Close();
            }
        }

        private static void AssertInvalidCandidateIsNonSelecting()
        {
            var window = CreateWindow();
            try
            {
                Assert.That(window.TrySelectCandidate(999), Is.False);
                Assert.That(window.LastReport.SelectedCandidate, Is.Null);
                Assert.That(window.LastReport.Issues.Select(issue => issue.Code),
                    Does.Contain("CANDIDATE_INDEX_INVALID"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(window);
            }
        }
    }
}
