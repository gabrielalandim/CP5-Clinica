using Clinica.Application.Dtos;

namespace Clinica.Application.Common;

/// <summary>Resposta paginada padronizada (itens + metadados + links HATEOAS).</summary>
public class PageResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
    public List<LinkDto> Links { get; set; } = new();

    public static PageResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalItems) =>
        new() { Items = items, Page = page, PageSize = pageSize, TotalItems = totalItems };
}
