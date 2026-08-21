using System.Collections.Generic;
using System.Linq;
using Unity.Services.DeploymentApi.Editor;

namespace Unity.Services.Multiplay.Authoring.Core.Deployment
{
    static class DuplicateResourceValidation
    {
        public static IReadOnlyList<T> FilterDuplicateResources<T>(
            IReadOnlyList<T> resources,
            out IReadOnlyList<IGrouping<string, T>> duplicateGroups) where T : IDeploymentItem
        {
            duplicateGroups = resources
                .GroupBy(r => r.Name)
                .Where(g => g.Count() > 1)
                .ToList();

            var hashset = new HashSet<string>(duplicateGroups.Select(g => g.Key));

            return resources
                .Where(r => !hashset.Contains(r.Name))
                .ToList();
        }

        public static (string, string) GetDuplicateResourceErrorMessages(
            DeploymentItem targetResource,
            IReadOnlyList<DeploymentItem> group)
        {
            var duplicates = group
                .Except(new[] { targetResource })
                .ToList();

            var duplicatesStr = string.Join(", ", duplicates.Select(d => $"'{d.Path}'"));
            var shortMessage = $"'{targetResource.Name}' was found duplicated in other files: {duplicatesStr}";
            var message = $"Multiple resources with the same identifier '{targetResource.Name}' were found. "
                + "Only a single resource for a given identifier may be deployed/fetched at the same time. "
                + "Give all resources unique identifiers or deploy/fetch them separately to proceed.\n"
                + shortMessage;
            return (shortMessage, message);
        }
    }
}
