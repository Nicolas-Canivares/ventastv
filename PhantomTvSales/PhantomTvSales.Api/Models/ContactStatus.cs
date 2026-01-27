namespace PhantomTvSales.Api.Models;

public enum ContactStatus
{
    AwaitingResponse = 1,   // contactado a la espera de respuesta
    SaleClosed = 2,         // contactado y cerrada la venta
    NotInterested = 3,      // contactado y no quiere el servicio
    ContactLater = 4        // contactado servicio luego
}
