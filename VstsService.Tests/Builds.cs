using System;
using System.Linq;
using System.Threading.Tasks;
using SecurePipelineScan.VstsService.Requests;
using Shouldly;
using Xunit;

namespace SecurePipelineScan.VstsService.Tests
{
    public class Builds : IClassFixture<TestConfig>
    {
        private readonly TestConfig _config;
        private readonly IVstsRestClient _client;

        public Builds(TestConfig config)
        {
            _config = config;
            _client = new VstsRestClient(_config.Organization, _config.Token);
        }
        
        [Fact]
        [Trait("category", "integration")]
        public async Task QueryBuildDefinition()
        {
            var buildDefinition =
                await _client.GetAsync(Requests.Builds.BuildDefinition(_config.Project, _config.BuildDefinitionId));

            buildDefinition.ShouldNotBeNull();
            buildDefinition.Id.ShouldNotBeNull();
            buildDefinition.Name.ShouldNotBeNull();
            buildDefinition.Project.ShouldNotBeNull();
            buildDefinition.Process.Type.ShouldNotBe(0);
            buildDefinition.Process.Phases.First().Steps.First().Task.Id.ShouldNotBeNull();
            buildDefinition.Repository.ShouldNotBeNull();
            buildDefinition.Repository.Url.ShouldNotBeNull();
        }

        [Fact]
        public async Task QueryBuildDefinitionsReturnsBuildDefinitionsWithTeamProjectReference()
        {
            var projectId = (await _client.GetAsync(Project.Properties(_config.Project))).Id;

            var buildDefinitions = _client.Get(Requests.Builds.BuildDefinitions(projectId)).ToList();

            buildDefinitions.ShouldNotBeNull();
            buildDefinitions.First().Id.ShouldNotBeNull();
            buildDefinitions.First().Project.Id.ShouldNotBeNull();
        }

        [Fact]
        public async Task QueryBuildDefinitionsReturnsBuildDefinitionsWithExtendedProperties()
        {
            var projectId = (await _client.GetAsync(Project.Properties(_config.Project))).Id;

            var buildDefinitions = await _client.GetAsync(Requests.Builds.BuildDefinitions(projectId, true).Request.AsJson());

            buildDefinitions.ShouldNotBeNull();
            buildDefinitions.SelectTokens("value[*].process").Count().ShouldBeGreaterThan(0);
        }
        
        [Fact]
        [Trait("category", "integration")]
        public async Task GetProjectRetentionSetting()
        {
            var retentionSettings = await _client.GetAsync(Requests.Builds.Retention(_config.Project))
                .ConfigureAwait(false);

            retentionSettings.ShouldNotBeNull();
            retentionSettings.PurgeRuns.ShouldNotBeNull();
            retentionSettings.PurgeRuns.Value.ShouldNotBe(0);
        }
    }
}