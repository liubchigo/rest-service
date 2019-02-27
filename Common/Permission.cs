namespace Common
{
    public class Permission

    {
        public PermissionId PermissionId { get; set; }
        public int PermissionBit { get; set; }
        public string NamespaceId { get; set; }
        public string DisplayName { get; set; }
        public Permission(int permissionBit, PermissionId permissionId)
        {
            PermissionId = permissionId;
            PermissionBit = permissionBit;
        }
    }
}