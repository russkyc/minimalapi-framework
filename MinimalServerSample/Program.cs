using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Russkyc.MinimalApi.Framework;
using Russkyc.MinimalApi.Framework.Core;
using Russkyc.MinimalApi.Framework.Core.Access;
using Russkyc.MinimalApi.Framework.Options;

FrameworkApiDocsOptions.EnableSidebar = true;
FrameworkOptions.EnableJwtAuthentication = true;
FrameworkOptions.EnableCookieAuthentication = true;
FrameworkOptions.EnableRoleBasedPermissions = true;
FrameworkOptions.JwtAudience = "localhost";
FrameworkOptions.JwtIssuer = "localhost";
FrameworkOptions.JwtKey = "00bebc5af5df8b9bac1ac530d597935a";

var app = MinimalApiFramework
    .CreateDefault(options => options.UseSqlite("Data Source=test.sqlite"));

app.MapPost("/login", (LoginRequest request) =>
{
    // Simple hardcoded validation for demo
    if (request is not ({ Username: "admin", Password: "admin" } or { Username: "user", Password: "user" })) return Results.Unauthorized();
    var claims = new[]
    {
        new Claim(ClaimTypes.Name, request.Username),
        new Claim(ClaimTypes.Role, request.Username.Equals("admin") ? "Admin" : "User")
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FrameworkOptions.JwtKey!));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: FrameworkOptions.JwtIssuer,
        audience: FrameworkOptions.JwtAudience,
        claims: claims,
        expires: DateTime.Now.AddHours(1),
        signingCredentials: credentials);

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
});

app.MapPost("/login-cookie", async (LoginRequest request, HttpContext context) =>
{
    // Simple hardcoded validation for demo
    if (request is not ({ Username: "admin", Password: "admin" } or { Username: "user", Password: "user" })) return Results.Unauthorized();
    var claims = new[]
    {
        new Claim(ClaimTypes.Name, request.Username),
        new Claim(ClaimTypes.Role, request.Username.Equals("admin") ? "Admin" : "User")
    };
    
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    return Results.Ok("Logged in with cookie");
});

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok("Logged out");
});

app.Run();

public class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
}

[RequirePermission(ApiMethod.Post, "xcx")]
[RequirePermission(ApiMethod.Get, "xcv")]
[RequireRoles(ApiMethod.Post, "Admin")]
[RequireRoles(ApiMethod.Get, "Admin", "User")]
public class SampleEmbeddedEntity : DbEntity<int>
{
    public required string Property2 { get; set; }
}

public class SampleEntity : DbEntity<Guid>
{
    [Required, MinLength(5)] public required string Property { get; set; }
    public virtual SampleEmbeddedEntity? EmbeddedEntity { get; set; }
}