// ADJUSTED ↓
namespace AviFinal.Api.Models
{
    public interface IInspectWagonEntity
    {
        int WagonNumber { get; set; }

        int Id { get; set; }

        string? RefurbishValue { get; set; }

        string? MissingValue { get; set; }

        string? ReplaceValue { get; set; }

        string? MissingPhoto { get; set; }

        string? ReplacePhoto { get; set; }

        string? LaborValue { get; set; }
    }
}
