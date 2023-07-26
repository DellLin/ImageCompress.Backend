public class GoogleAuth
{
    [JsonProperty("aud")]
    public string? Aud { get; set; }

    [JsonProperty("exp")]
    public string? Exp { get; set; }

    [JsonProperty("iat")]
    public string? Iat { get; set; }

    [JsonProperty("iss")]
    public string? Iss { get; set; }

    [JsonProperty("sub")]
    public string? Sub { get; set; }

    [JsonProperty("email")]
    public string? Email { get; set; }

    [JsonProperty("email_verified")]
    public bool? EmailVerified { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("picture")]
    public string? Picture { get; set; }
}
