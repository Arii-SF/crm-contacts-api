namespace CrmContactsApi.DTOs
{
    public class VentasClienteResponse
    {
        public decimal TotalVentas { get; set; }
        public int CantidadVentas { get; set; }
        public DateTime? UltimaVenta { get; set; }
        public int ValorCliente { get; set; }
        public string CategoriaCliente { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
    }

    public class TopClienteDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal TotalVentas { get; set; }
    }

    public class VentaDto
    {
        public int Id { get; set; }
        public string NumeroVenta { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    public class ClienteVentasDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Dpi { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}