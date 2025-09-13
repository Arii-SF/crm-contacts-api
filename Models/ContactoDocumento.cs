public class ContactoDocumento
{
    public int Id { get; set; }
    public int ContactoId { get; set; }
    public string NombreArchivo { get; set; }
    public string NombreOriginal { get; set; }
    public string RutaArchivo { get; set; }
    public string TipoArchivo { get; set; }
    public long TamañoArchivo { get; set; }
    public string UsuarioSubida { get; set; }
    public DateTime FechaSubida { get; set; }
    public string Descripcion { get; set; }
}