using System;
using System.Collections.Generic;
using SecurePipelineScan.VstsService.Response;

namespace SecurePipelineScan.VstsService.Requests
{
    public static class Builds
    {
        public static IEnumerableRequest<BuildDefinition> BuildDefinitions(string projectId, bool includeAllProperties) =>
            new VstsRequest<BuildDefinition>(
                $"{projectId}/_apis/build/definitions", new Dictionary<string, object>
                {
                    {"includeAllProperties", $"{includeAllProperties}"},
                    {"api-version", "5.0-preview.7"}
                }).AsEnumerable();

        public static IEnumerableRequest<BuildDefinition> BuildDefinitions(string projectId) =>
            BuildDefinitions(projectId, false);

        public static IVstsRequest<BuildDefinition> BuildDefinition(string projectId, string id) =>
            new VstsRequest<BuildDefinition>($"{projectId}/_apis/build/definitions/{id}");

        public static IVstsRequest<ProjectRetentionSetting> Retention(string project) =>
            new VstsRequest<ProjectRetentionSetting>($"{project}/_apis/build/retention");
    }
}