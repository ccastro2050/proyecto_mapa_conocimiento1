namespace ApiMapa.Excepciones;

/// <summary>
/// Excepción de la capa de negocio. Permite que el servicio comunique "esto no
/// existe, o está inactivo" sin mencionar HTTP ni el código 404: traducir a HTTP
/// es trabajo del controlador (Artículo 3).
/// </summary>
public class NoEncontradoExcepcion : Exception
{
    public NoEncontradoExcepcion(string mensaje) : base(mensaje)
    {
    }
}
