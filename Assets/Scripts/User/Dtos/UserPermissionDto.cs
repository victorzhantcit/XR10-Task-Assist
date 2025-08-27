using System.Collections.Generic;

#nullable enable 
namespace User.Dtos
{
    public class UserPermissionDto
    {
        public string? State { get; set; }
        public string? Message { get; set; }
        public Personal? Personal { get; set; }
        public RolePermission? Permissions { get; set; }
        public List<BuildingInformation>? BuildingsInfo { get; set; }
        public string? DateTime { get; set; }
    }

    //個人資料
    public class Personal
    {
        public int? Id { get; set; }
        public string? Account { get; set; }
        //public string? Role { get; set; }
        public string? DisplayName { get; set; }
        public int? RoleId { get; set; }
        public int? UpperId { get; set; }
        public string? Department { get; set; }
        public string? Email { get; set; }
        public string? Tel { get; set; }
        public string? DefaultPage { get; set; }
        public string? DefaultBuilding { get; set; }
        public string? Language { get; set; }
    }

    //角色權限
    public class RolePermission
    {
        public List<Building>? BuildingPermission { get; set; }
        public List<Page>? PagePermission { get; set; }
    }

    //建築權限
    public class Building
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public ClientPermission? Permissions { get; set; }
    }

    //頁面權限
    public class Page
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Router { get; set; }
        public string? Upper { get; set; }
        public ClientPermission? Permissions { get; set; }
    }

    //權限設定 Viewable可見 / Enable可操作 / Editable可編輯 / Ctrlable可控制
    public class ClientPermission
    {
        public bool? Viewable { get; set; }
        public bool? Enable { get; set; }
        public bool? Editable { get; set; }
        public bool? Ctrlable { get; set; }
    }
    //建築詳細資訊
    public class BuildingInformation
    {
        public string? Project { get; set; }
        public string? CodeName { get; set; }
        public string? Name { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? ModelUrn { get; set; }
        public Information? Information { get; set; }
    }
    //各資訊物件
    public class Information
    {
        public string? Image { get; set; }
        public string? Video { get; set; }
        public List<Introduction>? Introduction { get; set; }
        public dynamic? SystemSetting { get; set; }
        public dynamic? AreaSetting { get; set; }
        public dynamic? ElectricityBillSetting { get; set; }
    }


    public class Introduction
    {
        public string? Title { get; set; }
        public string? Value { get; set; }
    }
}
