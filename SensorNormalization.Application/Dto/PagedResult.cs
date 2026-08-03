namespace SensorNormalization.Application.Dto;

// Sayfali sonuc sarmalayici (odev 8.3 formati).
// pageIndex/pageSize konvansiyonu tum sistemde tek ve sabittir (odev 8.4).
public class PagedResult<T>
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
}
