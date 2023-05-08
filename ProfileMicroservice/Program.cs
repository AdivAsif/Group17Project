using System.Security.Claims;
using System.Text;
using AutoMapper;
using Group17profile;
using Group17profile.helpers;
using Group17profile.Middleware;
using Group17profile.Models;
using Group17profile.Repositories;
using Group17profile.Services;
using Group17profile.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
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
builder.Services.Configure<ConnectionStrings>(builder.Configuration.GetSection("ConnectionStrings"));
builder.Services.AddSingleton(c => new ProfileDbContext(c.GetRequiredService<IOptions<ConnectionStrings>>()));
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo {Title = "Group17ProfileMicroservice", Version = "v1"});
});
var tokenSettings = builder.Configuration.GetSection("TokenSettings").Get<TokenSettings>();
var key = Encoding.ASCII.GetBytes(tokenSettings!.Secret);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(g =>
{
    g.OperationFilter<AuthResponsesOperationFilter>();
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
    g.UseAllOfToExtendReferenceSchemas();
});
builder.Services.AddSwaggerGenNewtonsoftSupport();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOutputCache();
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
            corsPolicyBuilder.WithOrigins(builder.Configuration.GetSection("Group17Website")["BaseUrl"] ?? string.Empty)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Automapper
var mapperConfig = new MapperConfiguration(mc => { mc.AddProfile(new AutoMapperProfile()); });
var mapper = mapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

// Add repositories in /Repositories
builder.Services.AddTransient<IProfileRepository, ProfileRepository>();

// Add services in /Services
builder.Services.AddTransient<IProfileService, ProfileService>();
builder.Services.AddTransient<IStorageService, StorageService>();

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
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Group 17 Profile Microservice v1");
    options.RoutePrefix = string.Empty;
    options.ConfigObject = swaggerOptions.ConfigObject;
});
app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseOutputCache();

app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

app.Run();