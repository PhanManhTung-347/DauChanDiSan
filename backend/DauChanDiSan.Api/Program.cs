using DauChanDiSan.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddBackend(builder.Configuration);

var app = builder.Build();
app.UseBackend();
app.Run();
