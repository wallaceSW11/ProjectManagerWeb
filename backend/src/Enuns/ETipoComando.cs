using System.Text.Json.Serialization;

namespace ProjectManagerWeb.src.Enuns;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ETipoComando
{
    INSTALAR,
    INICIAR,
    BUILDAR,
    ABRIR_NA_IDE
}