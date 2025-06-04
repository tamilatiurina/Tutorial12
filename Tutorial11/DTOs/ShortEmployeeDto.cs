namespace Tutorial11.DTOs;

public class ShortEmployeeDto
{
    public int Id { get; set; }
    public string FullName { get; set; }

    public ShortEmployeeDto(int id, string fullName)
    {
        Id = id;
        FullName = fullName;
    }
}