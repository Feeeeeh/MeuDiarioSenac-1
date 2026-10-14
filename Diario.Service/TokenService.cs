using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

public class TokenService 
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GerarToken(Usuario usuario)
    {
        var jwtSettings = _configuration.GetSection("JWTSettings");
        var handler = new JwtSecurityTokenHandler();
        var chaveSecretaBytes = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);
        var credenciais = new SigningCredentials(new SymmetricSecurityKey(chaveSecretaBytes), SecurityAlgorithms.HmacSha256Signature);
        var claims  = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.Nome),
        };
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = credenciais,
            Expires = DateTime.UtcNow.AddHours(double.Parse(jwtSettings["ExpirationInHours"]))
        };
        
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);


    }
}