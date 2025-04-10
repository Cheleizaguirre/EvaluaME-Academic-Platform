public class AspNetRole
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required ICollection<AspNetUserRole> UserRoles { get; set; }
}

public class AspNetUserRole
{
    public required string UserId { get; set; }
    public required string RoleId { get; set; }
    public required AspNetRole Role { get; set; }
}
