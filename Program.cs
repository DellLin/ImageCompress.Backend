using System.Text;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using static ImageCompress.AccountSQL.AccountService;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference=new OpenApiReference{Id="Bearer",Type=ReferenceType.SecurityScheme},
            },
            Array.Empty<string>()
        }
    });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddCookie()
        .AddJwtBearer(options =>
        {
            // 當驗證失敗時，回應標頭會包含 WWW-Authenticate 標頭，這裡會顯示失敗的詳細錯誤原因
            options.IncludeErrorDetails = true; // 預設值為 true，有時會特別關閉
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // 透過這項宣告，就可以從 "sub" 取值並設定給 User.Identity.Name
                NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                // 透過這項宣告，就可以從 "roles" 取值，並可讓 [Authorize] 判斷角色
                RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                // 一般我們都會驗證 Issuer
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer"),
                // 通常不太需要驗證 Audience
                ValidateAudience = false,
                // 一般我們都會驗證 Token 的有效期間
                ValidateLifetime = true,
                // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
                ValidateIssuerSigningKey = false,
                // "1234567890123456" 應該從 IConfiguration 取得
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtSettings:SignKey")!))
            };
        });
builder.Services.AddGrpcClient<AccountServiceClient>((serviceProvider, options) =>
{
    if (builder.Environment.IsDevelopment())
    { options.Address = new Uri("http://localhost:5243"); }
    else
    { options.Address = new Uri("https://imagecompress-account-sql-iaxnu4eisa-de.a.run.app:443"); }
});

builder.Services.AddSingleton<KmsHelper>();
builder.Services.AddSingleton<JwtHelper>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
