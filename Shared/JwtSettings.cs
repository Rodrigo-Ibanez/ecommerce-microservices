namespace Shared
{
    public class JwtSettings
    {
        public string Key { get; set; } = "chave-secreta-super-segura";
        public string Issuer { get; set; } = "EcommerceAuth";
        public string Audience { get; set; } = "EcommerceUsers";
        public int ExpirationMinutes { get; set; } = 60;
    }
}