using System.Linq;
using System.Threading.Tasks;
using SecurePipelineScan.VstsService.Permissions;
using SecurePipelineScan.VstsService.Requests;
using Shouldly;
using Xunit;

namespace SecurePipelineScan.VstsService.Tests
{
    [Trait("Category", "integration")]
    public class YamlPipelineTests : IClassFixture<TestConfig>
    {
        private readonly TestConfig _config;
        private readonly IVstsRestClient _client;

        public YamlPipelineTests(TestConfig config)
        {
            _config = config;
            _client = new VstsRestClient(config.Organization, config.Token);
        }

        [Fact(Skip = "Not using the request")]
        public async Task CanValidateYamlPipeline()
        {
            var response = await _client.PostAsync(YamlPipeline.Parse(_config.Project, _config.BuildDefinitionYamlId),
                    new YamlPipeline.YamlPipelineRequest()
                ).ConfigureAwait(false);
            Assert.NotNull(response.FinalYaml);
        }
    }
}