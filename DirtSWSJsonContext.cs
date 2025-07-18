using System.Text.Json.Serialization;



public class ErrorResponse
{
    public ErrorDetail? error { get; set; }
}

public class ErrorDetail
{
    public string? message { get; set; }
    public string? type { get; set; }
}

[JsonSerializable(typeof(ErrorResponse))]
public partial class DirtSWSJsonContext : JsonSerializerContext { }

