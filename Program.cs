
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TodoApi;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ToDoDbContext>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAnyOrigin",
                      builder =>
                      {
                          builder.AllowAnyOrigin()
                                 .AllowAnyMethod()
                                 .AllowAnyHeader()
                                 .DisallowCredentials()
                                 .SetPreflightMaxAge(TimeSpan.FromSeconds(0));
                      });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());});
builder.Services.AddSwaggerGen(options =>
{
    // options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    // {
    //     Scheme = "Bearer",
    //     BearerFormat = "JWT",
    //     In = ParameterLocation.Header,
    //     Name = "Authorization",
    //     Description = "Bearer Authentication with JWT Token",
    //     Type = SecuritySchemeType.Http
    // });
    // options.AddSecurityRequirement(new OpenApiSecurityRequirement
    // {
    //     {
    //         new OpenApiSecurityScheme
    //         {
    //     Reference = new OpenApiReference
    //             {
    //                 Id = "Bearer",
    //                 Type = ReferenceType.SecurityScheme
    //             }
    //         },
    //         new List<string>()
    //     }
    // });
}
);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "JWT:Issuer",
            ValidAudience = "JWT:Audience",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("JWT:KEYJWtLONGrrrrrrrrrrrrrrrr"))
        };
    });
builder.Services.AddAuthorization();


builder.Services.AddDistributedMemoryCache();

   builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });


builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

var app = builder.Build();

// Configure authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization(); // Add these two lines

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseCors("CorsPolicy");
app.UseSession();
app.MapGet("/getTodos", async (ToDoDbContext db) =>
    await db.Items.ToListAsync()
);
// .RequireAuthorization();
app.MapPost("/addTodo", async (Item todo, ToDoDbContext db) =>
{
    db.Items.Add(todo);
    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});
// .RequireAuthorization();
app.MapPut("/updateTodo/{id}", async (int id, [FromQuery] string isComplete, ToDoDbContext db) =>
{
    var todo = await db.Items.FindAsync(id);

    if (todo is null) return Results.NotFound();

    if(isComplete is not null)
       todo.IsComplete = isComplete=="true"?true:false;

    await db.SaveChangesAsync();

    return Results.Created($"/todoitems/{todo.Id}", todo);
});
// .RequireAuthorization();
app.MapDelete("/deleteTodo/{id}", async (int id, ToDoDbContext db) =>
{
    if (await db.Items.FindAsync(id) is Item todo)
    {
        db.Items.Remove(todo);
        await db.SaveChangesAsync();
        return Results.Ok(todo);
    }

    return Results.NotFound();
});
// .RequireAuthorization();
app.MapPost("/login", async ([FromQuery] string name,[FromQuery] string password, ToDoDbContext db, IHttpContextAccessor httpContextAccessor) =>
{
    var user = db.Users?.FirstOrDefault(u => u.Name == name && u.Password == password);
    if (user is not null)
    {
        var jwt = CreateJWT(user);
        AddSession(user, httpContextAccessor);
        await db.SaveChangesAsync();
        return Ok(jwt);
    }
    return Unauthorized();
});
app.MapPost("/register", async (User user, ToDoDbContext db, IHttpContextAccessor httpContextAccessor) =>
{
    db.Users.Add(user);
    var jwt = CreateJWT(user);
     AddSession(user, httpContextAccessor);
     await db.SaveChangesAsync();
    return Ok(jwt);   
});
void AddSession(User user, IHttpContextAccessor httpContextAccessor)
{
    httpContextAccessor.HttpContext.Session.SetString("UserId", user.Id.ToString());
    httpContextAccessor.HttpContext.Session.SetString("UserName", user.Name);
}
IActionResult Unauthorized()
{
    return new UnauthorizedResult();
}
ActionResult Ok(object jwt)
{
    return new OkObjectResult(jwt);
}
object CreateJWT(User user)
{
    var claims = new[]
    {
       new Claim("id", user.Id.ToString()),
        new Claim("name", user.Name),
        new Claim("password", user.Password),  
    };
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("JWT:KEYJWtLONGrrrrrrrrrrrrrrrr"));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: "JWT:Issuer",
        audience: "JWT:Audience",
        claims: claims,
        expires: DateTime.Now.AddDays(7),
        signingCredentials: creds);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
app.Run();
