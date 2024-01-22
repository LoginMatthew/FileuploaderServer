using FileUploaderServer;
using FileUploaderServer.Context;
using FileUploaderServer.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);



builder.WebHost.ConfigureKestrel(options => options.Listen(System.Net.IPAddress.Parse(GlobalValues.IPAddress), GlobalValues.portNumber));
//builder.WebHost.ConfigureKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 5003));

var connecttionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DataFileDbContext>(options =>
{
    options.UseMySQL(connecttionString);
});
builder.Services.AddDbContext<UserDbContext>(options =>
{
    options.UseMySQL(connecttionString);
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

builder.Services.Configure<FormOptions>(x =>
{
    x.ValueLengthLimit = int.MaxValue;
    x.MultipartBoundaryLengthLimit = int.MaxValue;
    x.MemoryBufferThreshold = int.MaxValue;
});

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = GlobalValues.httpAddress,
            ValidAudience = GlobalValues.httpAddress,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GlobalValues.secretKey))
        };
    });


builder.Services.AddControllers();

builder.Services.AddTransient<ITokenService, TokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}
app.UseHttpsRedirection();
app.UseCors("CorsPolicy");


app.UseStaticFiles();

#region Checking whether the folder is exist or not
if (!Directory.Exists("Resources"))
{
    Directory.CreateDirectory("Resources");
}

if (!Directory.Exists("Resources\\Files"))
{
    Directory.CreateDirectory("Resources\\Files");
}
#endregion

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"Resources")),
    RequestPath = new PathString("/Resources")
});
 
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
