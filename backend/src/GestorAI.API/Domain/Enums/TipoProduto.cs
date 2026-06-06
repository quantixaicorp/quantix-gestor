using System.Text.Json.Serialization;

namespace GestorAI.API.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoProduto { Produto, Servico }
