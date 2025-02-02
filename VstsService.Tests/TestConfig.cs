using System;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SecurePipelineScan.VstsService.Tests
{
    public class TestConfig
    {
        public TestConfig()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.user.json", true)
                .AddEnvironmentVariables()
                .Build();
                
            configuration.Bind(this);
        }

        public string Token { get; set; }
        public string Project { get; set; }
        public string ProjectId { get; set; }
        public string Organization { get; set; }
        public string ExpectedAgentPoolName { get; set; } = "Default";
        public string ReleaseDefinitionId { get; set; }
        public string ReleaseDefinitionName { get; set; }
        public int AgentPoolId { get; set; }
        public int AgentQueueId { get; set; }
        public string BuildId { get; set; }
        public string BuildDefinitionId { get; set; }
        public string BuildDefinitionYamlId { get; set; }
        public string RepositoryId { get; set; }
        public string GitItemFilePath { get; set; }
        public string EntitlementUser { get; set; } = Encoding.UTF8.GetString(Convert.FromBase64String("UmljaGFyZC5PcHJpbnNAcmFib2Jhbmsubmw=")); // some obfuscation to hide the e-mail address
    }
}