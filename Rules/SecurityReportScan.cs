using System.Collections;
using System.Collections.Generic;
using Rules.Reports;
using SecurePipelineScan.Rules.Checks;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System.Linq;
using Repository = SecurePipelineScan.VstsService.Response.Repository;

namespace SecurePipelineScan.Rules
{
    public class SecurityReportScan
    {
        private readonly IVstsRestClient client;

        public SecurityReportScan(IVstsRestClient client)
        {
            this.client = client;
        }

        public void Execute()
        {
            var projects = client.Get(Project.Projects()).Value;
            foreach (var project in projects)
            {
                Execute(project.Name);
            }
        }
        

        public SecurityReport Execute(string project)
        {
            var applicationGroups =
                client.Get(ApplicationGroup.ApplicationGroups(project)).Identities;

            var namespaceId = client.Get(SecurityNamespace.SecurityNamespaces()).Value
                .First(ns => ns.DisplayName == "Git Repositories").NamespaceId;
                
            var applicationGroupId = applicationGroups
                .First(gi => gi.DisplayName == $"[{project}]\\Project Administrators").TeamFoundationId;
         
            var projectId = client.Get(Project.Properties(project)).Id;           
            
            var permissionsGitRepositorySet = client.Get(PermissionsGroupRepositories.PermissionsGroupRepositorySet(
                projectId, namespaceId, applicationGroupId));
            
            var repositories = client.Get(VstsService.Requests.Repository.Repositories(project)).Value;

            
            var securityReport = new SecurityReport
            {
                Project = project,
                ApplicationGroupContainsProductionEnvironmentOwner =
                    ProjectApplicationGroup.ApplicationGroupContainsProductionEnvironmentOwner(applicationGroups),
                
                ProjectAdminHasNoPermissionToDeleteRepositorySet = 
                    Permission.HasNoPermissionToDeleteRepository(permissionsGitRepositorySet.Permissions),
                ProjectAdminHasNoPermissionToDeleteRepositories = 
                    ProjectAdminHasNoPermissionToDeleteRepositories(repositories, projectId, namespaceId, applicationGroupId),
                
                ProjectAdminHasNoPermissionToManagePermissionsRepositorySet = 
                    Permission.HasNoPermissionToManageRepositoryPermissions(permissionsGitRepositorySet.Permissions),
                ProjectAdminHasNoPermissionToManagePermissionsRepositories = 
                    ProjectAdminHasNoPermissionToManagePermissionsRepositories(repositories, projectId, namespaceId, applicationGroupId)
            };

            securityReport.ProjectIsSecure = (
                securityReport.ApplicationGroupContainsProductionEnvironmentOwner &&
                securityReport.ProjectAdminHasNoPermissionToDeleteRepositories &&
                securityReport.ProjectAdminHasNoPermissionToDeleteRepositorySet &&
                securityReport.ProjectAdminHasNoPermissionToManagePermissionsRepositories &&
                securityReport.ProjectAdminHasNoPermissionToManagePermissionsRepositorySet);

           return securityReport;
        }

        private bool CheckRabobankProjectAdminInProjectAdminGroup(string project, IEnumerable<VstsService.Response.ApplicationGroup> applicationGroups)
        {
            bool projectAdminHasOnlyRaboProjectAdminGroup = false;
            var groupid = applicationGroups.Single(x => x.FriendlyDisplayName == "Project Administrators").TeamFoundationId;
            var groupmembers = client.Get(ApplicationGroup.GroupMembers(project, groupid)).Identities;
            var count = groupmembers.Count();


            if (count == 1)
            {
                var applicationGroup = groupmembers.First(x => x.FriendlyDisplayName == "Rabobank Project Administrators");
                projectAdminHasOnlyRaboProjectAdminGroup = applicationGroup != null;
            }

            return projectAdminHasOnlyRaboProjectAdminGroup;
        }

        private bool ProjectAdminHasNoPermissionToManagePermissionsRepositories(IEnumerable<Repository> repositories, string projectId, string namespaceId, string applicationGroupId)
        {
            return repositories.All
            (r => 
                Permission.HasNoPermissionToManageRepositoryPermissions(
                    client.Get(PermissionsGroupRepositories.PermissionsGroupRepository(
                            projectId, namespaceId, applicationGroupId, r.Id))
                        .Permissions)
                == true);
        }

        private bool ProjectAdminHasNoPermissionToDeleteRepositories(IEnumerable<Repository> repositories, string projectId, string namespaceId, string applicationGroupId)
        {
            return repositories.All
            (r => 
                Permission.HasNoPermissionToDeleteRepository(
                    client.Get(PermissionsGroupRepositories.PermissionsGroupRepository(
                            projectId, namespaceId, applicationGroupId, r.Id))
                        .Permissions)
                == true);
        }
    }
}