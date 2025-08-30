namespace CrmContactsApi.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navegación
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}