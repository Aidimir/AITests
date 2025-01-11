using System.ComponentModel.DataAnnotations;

namespace Dal;

public class ReqRespModel
{
    [Key] public required Guid Id { get; set; }

    public required Guid UserId { get; set; }
    public string FileContent { get; set; }
    public string ResponseContent { get; set; }
}