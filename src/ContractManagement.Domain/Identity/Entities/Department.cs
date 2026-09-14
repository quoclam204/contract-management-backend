namespace ContractManagement.Domain.Identity.Entities;

public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Department()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public Department(string name, Guid? managerId = null) : this()
    {
        Name = name;
        ManagerId = managerId;
    }
}
