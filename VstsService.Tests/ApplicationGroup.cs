using Shouldly;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SecurePipelineScan.VstsService.Tests
{
    [Trait("category", "integration")]
    public class ApplicationGroup : IClassFixture<TestConfig>
    {
        private readonly TestConfig _config;
        private readonly IVstsRestClient _client;

        public ApplicationGroup(TestConfig config)
        {
            _config = config;
            _client = new VstsRestClient(config.Organization, config.Token);
        }

        [Fact]
        public async Task QueryApplicationGroupsOrganization()
        {
            var identity = await _client.GetAsync(Requests.ApplicationGroup.ApplicationGroups());
            identity.ShouldNotBeNull();
            identity.Identities.ShouldNotBeEmpty();

            var group = identity.Identities.First();
            group.DisplayName.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task QueryApplicationGroupsProject()
        {
            var identity = await _client.GetAsync(Requests.ApplicationGroup.ApplicationGroups(_config.Project));
            identity.ShouldNotBeNull();
            identity.Identities.ShouldNotBeEmpty();

            var group = identity.Identities.First();
            group.DisplayName.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task ExplicitIdentitiesForReposShouldGetIdentities()
        {
            var explicitIdentities = await _client.GetAsync(Requests.ApplicationGroup.ExplicitIdentitiesRepos(_config.ProjectId, "2e9eb7ed-3c0a-47d4-87c1-0ffdd275fd87", _config.RepositoryId));
            explicitIdentities.ShouldNotBeNull();
        }

        [Fact]
        public async Task ExplicitIdentitiesForBranchShouldGetIdentities()
        {
            var explicitIdentities = await _client.GetAsync(Requests.ApplicationGroup.ExplicitIdentitiesMasterBranch(_config.ProjectId, "2e9eb7ed-3c0a-47d4-87c1-0ffdd275fd87", _config.RepositoryId));
            explicitIdentities.ShouldNotBeNull();
        }

        [Fact]
        public async Task ExplicitIdentitiesForBuildDefinitionShouldGetIdentities()
        {
            string nameSpaceId = "33344d9c-fc72-4d6f-aba5-fa317101a7e9";

            var explicitIdentities = await _client.GetAsync(Requests.ApplicationGroup.ExplicitIdentitiesPipelines(_config.ProjectId, nameSpaceId, _config.BuildDefinitionId));
            explicitIdentities.ShouldNotBeNull();
            explicitIdentities.Identities.ShouldNotBeEmpty();
        }
    }
}