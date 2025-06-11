using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Tutorial11.Models;

public class Device
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public bool IsEnabled { get; set; }
    
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
    
    /*[NotMapped]
    [JsonPropertyName("type")]
    public string Type { get; set; }*/
    [JsonPropertyName("typeId")]
    public int? DeviceTypeId { get; set; }
    public DeviceType DeviceType { get; set; }
    
    public ICollection<DeviceEmployee> DeviceEmployees { get; set; } = new List<DeviceEmployee>();
}