using System.Collections;
using System.Collections.Generic;
using Unity.Services.Multiplay.Authoring.Editor.Shared.Analytics;
using UnityEditor;
using UnityEngine;

namespace Unity.Services.Multiplay.Authoring.Editor
{
    interface IBuildsAnalytics
    {
        AnalyticsTimer BeginBuild();

        void BuildDetails(BuildTarget originalBuildTarget, StandaloneBuildSubtarget buildSubtarget, BuildTarget serverBuildTarget);
    }
}
