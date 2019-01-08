using System;
using System.Collections;
using System.Collections.Generic;
using Rules.Reports;
using SecurePipelineScan.Rules.Checks;
using SecurePipelineScan.VstsService;
using SecurePipelineScan.VstsService.Requests;
using System.Linq;
using SecurePipelineScan.VstsService.Response;
using ApplicationGroup = SecurePipelineScan.VstsService.Requests.ApplicationGroup;
using Permission = SecurePipelineScan.Rules.Checks.Permission;
using Project = SecurePipelineScan.VstsService.Requests.Project;
using Repository = SecurePipelineScan.VstsService.Response.Repository;
using SecurityNamespace = SecurePipelineScan.VstsService.Requests.SecurityNamespace;

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
            
            var groupMembers = getGroupMembersFromApplicationGroup(project, applicationGroups);

            var namespaceIdGitRepositories = client.Get(SecurityNamespace.SecurityNamespaces()).Value
                .First(ns => ns.DisplayName == "Git Repositories").NamespaceId;

            var namespaceIdBuild = client.Get(SecurityNamespace.SecurityNamespaces()).Value
                .First(ns => ns.Name == "Build").NamespaceId;
            
            var applicationGroupIdProjectAdmins = applicationGroups
                .First(gi => gi.DisplayName == $"[{project}]\\Project Administrators").TeamFoundationId;
         
            var applicationGroupIdBuildAdmins = applicationGroups
                .First(gi => gi.DisplayName == $"[{project}]\\Build Administrators").TeamFoundationId;
            
            
            var projectId = client.Get(Project.Properties(project)).Id;           
            
            
            var permissionsGitRepositorySet = client.Get(Permissions.PermissionsGroupRepositorySet(
                projectId, namespaceIdGitRepositories, applicationGroupIdProjectAdmins));

            var permissionsBuildProjectAdmins = client.Get(Permissions.PermissionsGroupSetId(
                projectId, namespaceIdBuild, applicationGroupIdProjectAdmins));
            
            var permissionsBuildBuildAdmins = client.Get(Permissions.PermissionsGroupSetId(
                projectId, namespaceIdBuild, applicationGroupIdBuildAdmins));

            
            var repositories = client.Get(VstsService.Requests.Repository.Repositories(projectId)).Value;

            var buildDefinitions = client.Get(VstsService.Requests.Builds.BuildDefinitions(projectId)).Value;

            
            var securityReport = new SecurityReport
            {
                Project = project,
                
                ApplicationGroupContainsProductionEnvironmentOwner =
                    ProjectApplicationGroup.ApplicationGroupContainsProductionEnvironmentOwner(applicationGroups),
                ProjectAdminGroupOnlyContainsRabobankProjectAdminGroup = 
                    ProjectApplicationGroup.ProjectAdministratorsGroupOnlyContainsRabobankProjectAdministratorsGroup(groupMembers),

                RepositoryRightsProjectAdmin = CheckRepositoryRights(permissionsGitRepositorySet.Permissions, 
                            repositories, projectId, namespaceIdGitRepositories, applicationGroupIdProjectAdmins),
                
                BuildRightsBuildAdmin = CheckBuildRights(permissionsBuildBuildAdmins.Permissions),
                BuildRightsProjectAdmin = CheckBuildRights(permissionsBuildProjectAdmins.Permissions), 
                
                BuildDefinitionsRightsBuildAdmin = CheckBuildDefinitionRights(buildDefinitions, projectId, namespaceIdBuild, applicationGroupIdBuildAdmins),
                BuildDefinitionsRightsProjectAdmin = CheckBuildDefinitionRights(buildDefinitions, projectId, namespaceIdBuild, applicationGroupIdProjectAdmins),
                
            };
          
           return securityReport;
        }

        private RepositoryRights CheckRepositoryRights(
            IEnumerable<SecurePipelineScan.VstsService.Response.Permission> permissions, 
            IEnumerable<Repository> repositories, string projectId, string namespaceId, string applicationGroupId)
        {
            return new RepositoryRights
            {
                HasNoPermissionToDeleteRepositories = 
                    ApplicationGroupHasNoPermissionToDeleteRepositories(repositories, projectId, namespaceId, applicationGroupId),
                HasNoPermissionToDeleteRepositorySet = 
                    Permission.HasNoPermissionToDeleteRepository(permissions),
                HasNoPermissionToManagePermissionsRepositories = 
                    ApplicationGroupHasNoPermissionToManagePermissionsRepositories(repositories, projectId, namespaceId, applicationGroupId),
                HasNoPermissionToManagePermissionsRepositorySet = 
                    Permission.HasNoPermissionToManageRepositoryPermissions(permissions)
            };
        }

        private BuildRights CheckBuildDefinitionRights(
            IEnumerable<BuildDefinition> buildDefinitions, string projectId, string namespaceId, string applicationGroupId)
        {
            return new BuildRights
            {
                HasNoPermissionsToDeleteBuilds =
                    ApplicationGroupHasNoPermissionToDeleteBuilds(buildDefinitions, projectId, namespaceId,
                        applicationGroupId),
                HasNoPermissionsToDeDestroyBuilds = 
                    ApplicationGroupHasNoPermissionToDestroyBuilds(buildDefinitions, projectId, namespaceId,
                        applicationGroupId),
                HasNoPermissionsToDeleteBuildDefinition = 
                    ApplicationGroupHasNoPermissionToDeleteBuildDefinition(buildDefinitions, projectId, namespaceId,
                        applicationGroupId),
                HasNoPermissionsToAdministerBuildPermissions = 
                    ApplicationGroupHasNoPermissionToAdministerBuildPermissions(buildDefinitions, projectId, namespaceId,
                        applicationGroupId),
            };
        }
            
        
        private BuildRights CheckBuildRights(
            IEnumerable<SecurePipelineScan.VstsService.Response.Permission> permissions)
        {
            return new BuildRights
            {
                HasNoPermissionsToAdministerBuildPermissions = 
                    Permission.HasNoPermissionToAdministerBuildPermissions(permissions),
                HasNoPermissionsToDeleteBuilds = 
                    Permission.HasNoPermissionToDeleteBuilds(permissions),
                HasNoPermissionsToDeDestroyBuilds = 
                    Permission.HasNoPermissionToDestroyBuilds(permissions),
                HasNoPermissionsToDeleteBuildDefinition = 
                    Permission.HasNoPermissionToDeleteBuildDefinition(permissions)
            };
        }
        
        private IEnumerable<VstsService.Response.ApplicationGroup> getGroupMembersFromApplicationGroup(string project, IEnumerable<VstsService.Response.ApplicationGroup> applicationGroups)
        {
            var groupId = applicationGroups.Single(x => x.DisplayName == $"[{project}]\\Project Administrators").TeamFoundationId;
            var groupMembers = client.Get(ApplicationGroup.GroupMembers(project, groupId)).Identities;
            return groupMembers;
        }

        private bool ApplicationGroupHasNoPermissionToManagePermissionsRepositories(IEnumerable<Repository> repositories, string projectId, string namespaceId, string applicationGroupId)
        {
            return repositories.All
            (r => 
                Permission.HasNoPermissionToManageRepositoryPermissions(
                    client.Get(Permissions.PermissionsGroupRepository(
                            projectId, namespaceId, applicationGroupId, r.Id))
                        .Permissions)
                == true);
        }

        private bool ApplicationGroupHasNoPermissionToDeleteRepositories(IEnumerable<Repository> repositories, string projectId, string namespaceId, string applicationGroupId)
        {
            return repositories.All
            (r => 
                Permission.HasNoPermissionToDeleteRepository(
                    client.Get(Permissions.PermissionsGroupRepository(
                            projectId, namespaceId, applicationGroupId, r.Id))
                        .Permissions)
                == true);
        }
        
        private bool ApplicationGroupHasNoPermissionToDeleteBuilds(IEnumerable<BuildDefinition> buildDefinitions, string projectId, string namespaceId, string applicationGroupId)
        {
            return buildDefinitions.All
            (r => 
                Permission.HasNoPermissionToDeleteBuilds(
                    client.Get(Permissions.PermissionsGroupSetIdDefinition(
                            projectId, namespaceId, applicationGroupId, r.id))
                        .Permissions)
                == true);
        }

        private bool ApplicationGroupHasNoPermissionToDeleteBuildDefinition(IEnumerable<BuildDefinition> buildDefinitions, string projectId, string namespaceId, string applicationGroupId)
        {
            return buildDefinitions.All
            (r => 
                Permission.HasNoPermissionToDeleteBuildDefinition(
                    client.Get(Permissions.PermissionsGroupSetIdDefinition(
                            projectId, namespaceId, applicationGroupId, r.id))
                        .Permissions)
                == true);
        }

        private bool ApplicationGroupHasNoPermissionToDestroyBuilds(IEnumerable<BuildDefinition> buildDefinitions, string projectId, string namespaceId, string applicationGroupId)
        {
            return buildDefinitions.All
            (r => 
                Permission.HasNoPermissionToDestroyBuilds(
                    client.Get(Permissions.PermissionsGroupSetIdDefinition(
                            projectId, namespaceId, applicationGroupId, r.id))
                        .Permissions)
                == true);
        }
        
        private bool ApplicationGroupHasNoPermissionToAdministerBuildPermissions(IEnumerable<BuildDefinition> buildDefinitions, string projectId, string namespaceId, string applicationGroupId)
        {
            return buildDefinitions.All
            (r => 
                Permission.HasNoPermissionToAdministerBuildPermissions(
                    client.Get(Permissions.PermissionsGroupSetIdDefinition(
                            projectId, namespaceId, applicationGroupId, r.id))
                        .Permissions)
                == true);
        }


    }
}