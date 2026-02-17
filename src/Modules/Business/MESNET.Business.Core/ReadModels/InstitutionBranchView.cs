namespace MESNET.Business.Core.ReadModels;

public class InstitutionBranchView
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string FieldCode { get; set; } = default!;
    public string FieldName { get; set; } = default!;
    public string EducationType { get; set; } = default!;
    public List<string> ActiveSpecializations { get; set; } = [];
    public bool IsActive { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
