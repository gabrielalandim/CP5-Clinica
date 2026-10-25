using Clinica.Application.Common;

namespace Clinica.Tests.Unit;

public class PageResultTests
{
    [Theory]
    [InlineData(25, 10, 3)]
    [InlineData(20, 10, 2)]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    public void TotalPages_EhCalculadoCorretamente(int totalItems, int pageSize, int esperado)
    {
        var page = PageResult<int>.Create(new List<int>(), 1, pageSize, totalItems);

        Assert.Equal(esperado, page.TotalPages);
    }

    [Fact]
    public void Hateoas_PrimeiraPagina_NaoTemPrev_MasTemNext()
    {
        var links = Hateoas.ForPage("/api/x", 1, 10, 3, "create_x");

        Assert.DoesNotContain(links, l => l.Rel == "prev");
        Assert.Contains(links, l => l.Rel == "next" && l.Href == "/api/x?page=2&pageSize=10");
        Assert.Contains(links, l => l.Rel == "last" && l.Href == "/api/x?page=3&pageSize=10");
    }

    [Fact]
    public void Hateoas_UltimaPagina_NaoTemNext()
    {
        var links = Hateoas.ForPage("/api/x", 3, 10, 3, "create_x");

        Assert.DoesNotContain(links, l => l.Rel == "next");
        Assert.Contains(links, l => l.Rel == "prev");
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(2, 500, 2, 50)]
    [InlineData(3, 5, 3, 5)]
    public void Normalizar_AjustaLimites(int page, int size, int pageEsperada, int sizeEsperado)
    {
        var (p, s) = Hateoas.Normalizar(page, size);

        Assert.Equal(pageEsperada, p);
        Assert.Equal(sizeEsperado, s);
    }
}
