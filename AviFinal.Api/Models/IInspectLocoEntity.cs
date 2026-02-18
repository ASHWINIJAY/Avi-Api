namespace AviAppFinal.Server.Models
{
    public interface IInspectLocoEntity
    {
        int LocoNumber { get; set; }

        int Id { get; set; }

        string? RefurbishValue { get; set; }

        string? MissingValue { get; set; }

        string? ReplaceValue { get; set; }

        string? MissingPhoto { get; set; }

        string? ReplacePhoto { get; set; }

        string? LaborValue { get; set; }
    }
}
