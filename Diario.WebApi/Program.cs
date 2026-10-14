using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var chaveSecretaBytes = Encoding.ASCII.GetBytes(builder.Configuration["JWTSettings:SecretKey"]);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var jwtSettings = builder.Configuration.GetSection("JWTSettings");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
}).AddJwtBearer("JwtBearer", options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(chaveSecretaBytes),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthorization();
app.UseAuthentication();


app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (ArgumentException ex)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { erro = ex.Message });
    }
    catch (Exception)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { erro = "Ocorreu um erro interno." });
    }
});

app.MapPost("/login/auth", ( [FromBody] Usuario usuario) =>
{
    TokenService authService = new TokenService(builder.Configuration);
    return authService.GerarToken(usuario);
});

var registrosGroup = app.MapGroup("/registros").RequireAuthorization();

registrosGroup.MapGet("/", () =>
{
    return new RegistroService().ListarRegistros();
});

registrosGroup.MapGet("/{id}", (int id) =>
{
    var registro = new RegistroService().BuscarPorId(id);
    return registro is null ? Results.NotFound() : Results.Ok(registro);
});

registrosGroup.MapPost("/", ([FromBody] Registro registro) =>
{
    new RegistroService().AdicionarRegistro(registro, registro.UsuarioId);
    return Results.Ok("Registro inserido com sucesso!");
});

registrosGroup.MapPut("/{id}", (int id, [FromBody] Registro registro) =>
{
    registro.Id = id;
    new RegistroService().AtualizarRegistro(registro);
    return Results.Ok("Registro atualizado com sucesso!");
});

registrosGroup.MapDelete("/{id}", (int id) =>
{
    var registro = new RegistroService().BuscarPorId(id);
    if (registro is null) return Results.NotFound();

    new RegistroService().RemoverRegistro(registro);
    return Results.Ok("Registro removido com sucesso!");
});

var usuariosGroup = app.MapGroup("/usuarios");

usuariosGroup.MapPost("/registrar", ([FromBody] Usuario usuario) =>
{
    new UsuarioService().RegistrarUsuario(usuario.Nome, usuario.Senha);
    return Results.Ok("Usuário registrado com sucesso!");
});

usuariosGroup.MapPost("/login", ([FromBody] Usuario usuario) =>
{
    var usuarioAutenticado = new UsuarioService().AutenticarUsuario(usuario.Nome, usuario.Senha);
    return Results.Ok($"Login realizado com sucesso! Bem-vindo, {usuarioAutenticado.Nome}.");
});

app.Run();