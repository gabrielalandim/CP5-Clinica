using Clinica.Application.Dtos;

namespace Clinica.Application.Common;

public static class Hateoas
{
    /// <summary>Links globais de uma página: self, first, prev, next, last e criação.</summary>
    public static List<LinkDto> ForPage(string baseUrl, int page, int pageSize, int totalPages, string createRel, string extraQuery = "")
    {
        string Url(int p) => $"{baseUrl}?page={p}&pageSize={pageSize}{extraQuery}";

        var links = new List<LinkDto>
        {
            new(Url(page), "self", "GET"),
            new(Url(1), "first", "GET")
        };

        if (page > 1) links.Add(new(Url(page - 1), "prev", "GET"));
        if (page < totalPages) links.Add(new(Url(page + 1), "next", "GET"));
        if (totalPages > 0) links.Add(new(Url(totalPages), "last", "GET"));

        links.Add(new(baseUrl, createRel, "POST"));
        return links;
    }

    public static (int Page, int PageSize) Normalizar(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;
        return (page, pageSize);
    }
}
