using System.Security.Claims;
using System.Text;
using AuthenticationMicroservice;
using AuthenticationMicroservice.Helpers;
using AuthenticationMicroservice.Middleware;
using AuthenticationMicroservice.Models;
using AuthenticationMicroservice.Repositories;
using AuthenticationMicroservice.Services;
using AuthenticationMicroservice.Settings;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOptions();
builder.Configuration.AddJsonFile("appsettings.Development.json", true, true);
builder.Configuration.AddEnvironmentVariables();
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));
builder.Services.Configure<FrontendStrings>(builder.Configuration.GetSection("FrontendStrings"));
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("EmailConfig"));
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.AddDbContext<AuthenticationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString") ??
                     throw new Exception("Could not connect to database.")));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo {Title = "AuthenticationMicroserviceAPI", Version = "v1"});
});
var tokenSettings = builder.Configuration.GetSection("TokenSettings").Get<TokenSettings>();
var key = Encoding.ASCII.GetBytes(tokenSettings!.Secret);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(g =>
{
    g.OperationFilter<ResponseOperationFilter>();
    g.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    g.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
                {Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = "Bearer"}},
            Array.Empty<string>()
        }
    });
});
builder.Services.AddSwaggerGenNewtonsoftSupport();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOutputCache();
builder.Services.AddLazyCache();
builder.Services.AddAuthentication(options => { options.DefaultScheme = "smart"; }).AddJwtBearer(j =>
{
    j.RequireHttpsMetadata = false;
    j.SaveToken = true;
    j.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    j.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            if (!string.IsNullOrEmpty(accessToken)) context.Token = accessToken;

            return Task.CompletedTask;
        }
    };
}).AddPolicyScheme("smart", "Authorization",
    options => { options.ForwardDefaultSelector = _ => JwtBearerDefaults.AuthenticationScheme; });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Constants.AuthorizationPolicies.HasUserId, policy => policy.RequireClaim(ClaimTypes.Sid));
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        corsPolicyBuilder =>
        {
            corsPolicyBuilder.WithOrigins(builder.Configuration.GetSection("FrontendStrings")["BaseUrl"] ?? string.Empty)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// AutoMapper
var mapperConfig = new MapperConfiguration(mc => { mc.AddProfile(new AutoMapperProfile()); });
var mapper = mapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

// Add repositories from /Repositories
builder.Services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>));

// Add services from /Services
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IAuthService, AuthService>();

var app = builder.Build();

var swaggerOptions = new SwaggerUIOptions
{
    RoutePrefix = string.Empty
};

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseSwagger();
app.UseAuthorization();
app.UseSwaggerAuthorized();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthenticationMicroservice Api v1");
    options.RoutePrefix = string.Empty;
    options.ConfigObject = swaggerOptions.ConfigObject;
});
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseOutputCache();

app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

app.Run();