namespace AviFinal.Api.DTO
{
    public class CockpitAllocationRequest
    {
        public string AssetType { get; set; } = null!;
        public List<int> TeamIds { get; set; } = new();
        public List<int> AssetIds { get; set; } = new();
    }
}
