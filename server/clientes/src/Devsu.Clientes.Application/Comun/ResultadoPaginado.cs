namespace Devsu.Clientes.Application.Comun;

/// <summary>Pagina de resultados junto con los datos necesarios para navegar.</summary>
public sealed record ResultadoPaginado<T>(
    IReadOnlyList<T> Items,
    int Pagina,
    int Tamanio,
    int Total)
{
    public int TotalPaginas => Tamanio <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamanio);

    public static ResultadoPaginado<T> Vacio(int pagina, int tamanio) =>
        new(Array.Empty<T>(), pagina, tamanio, 0);
}

/// <summary>Parametros de paginacion normalizados a rangos seguros.</summary>
public static class Paginacion
{
    public const int TamanioPorDefecto = 10;
    public const int TamanioMaximo = 100;

    public static (int Pagina, int Tamanio) Normalizar(int? pagina, int? tamanio)
    {
        var paginaFinal = pagina is null or < 1 ? 1 : pagina.Value;
        var tamanioFinal = tamanio switch
        {
            null or < 1 => TamanioPorDefecto,
            > TamanioMaximo => TamanioMaximo,
            _ => tamanio.Value,
        };

        return (paginaFinal, tamanioFinal);
    }
}
