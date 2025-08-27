using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Unity.Extensions;
using Unity.VisualScripting;
using User.Dtos;
using WebSocketSharp;

namespace User.Core
{
    public static class AuthService
    {
        private static string _iBMSPlatformServer = string.Empty;

        public static string LoginAPI_iBMSPlatform => $"{_iBMSPlatformServer}/api/Auth/Login";

        private static string PermAPI_iBMSPlatform => $"{_iBMSPlatformServer}/api/Auth/Login/Permission";

        public static void Initialize(string iBMSPlatformServer)
        {
            _iBMSPlatformServer = iBMSPlatformServer;
            APIHelper.RegisterLoginAPI(LoginAPI_iBMSPlatform);
        }

        public static async Task<bool> LoginUserOnIBMSPlatform(UserLoginIBMSPlatformDto userData)
        {
            var responseToken = await APIHelper.SendFormRequestAsync<string>(
                url: LoginAPI_iBMSPlatform,
                method: HttpMethod.POST,
                data: new List<KeyValue>
                {
                    new KeyValue("account", userData.Id),
                    new KeyValue("pw", userData.Password)
                },
                returnPureString: true
            );

            if (responseToken.IsSuccess)
                APIHelper.RegisterLoginAPI(LoginAPI_iBMSPlatform, responseToken.Data);

            return responseToken.IsSuccess;
        }

        public static async Task<UserPermissionDto> GetUserPermissionOnOnIBMSPlatform()
            => await APIHelper.SendServerFormRequestAsync<UserPermissionDto>(PermAPI_iBMSPlatform, HttpMethod.GET);

        public static UserRole GetUserRoleByPermission(UserPermissionDto userPermission)
        {
            // 身分與 API response query 對照表
            Dictionary<UserRole, string> _roleMap = new Dictionary<UserRole, string>
            {                
                { UserRole.Insp, "AppInsp" },
                { UserRole.Maint, "AppMaint" }
            };

            // 權限集合
            List<UserRole> permissionSet = new List<UserRole>();

            foreach (var (role, pageId) in _roleMap)
            {
                bool? editable = userPermission.Permissions?
                    .PagePermission?
                    .FirstOrDefault(p => p.Id == pageId)?
                    .Permissions?.Editable;

                if (editable == true)
                    permissionSet.Add(role);
            }

            if (permissionSet.Contains(UserRole.Maint) && permissionSet.Contains(UserRole.Insp))
                return UserRole.Staff;
            else if (permissionSet.Contains(UserRole.Insp)) 
                return UserRole.Insp;
            else if (permissionSet.Contains(UserRole.Maint)) 
                return UserRole.Maint;
            else 
                return UserRole.Undefined;
        }
    }
}
