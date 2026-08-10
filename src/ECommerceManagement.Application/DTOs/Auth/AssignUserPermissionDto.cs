namespace ECommerceManagement.Application.DTOs.Auth;

public class AssignUserPermissionDto
{
    public int UserId { get; set; }
    public int PermissionId { get; set; }
    public bool IsGranted { get; set; } // True ise ez ve izin ver, False ise ez ve yasakla
}