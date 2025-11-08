using Markdn.Api.Configuration;
using Markdn.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.Configure<MarkdnOptions>(
    builder.Configuration.GetSection("Markdn"));

// Add health checks
builder.Services.AddHealthChecks();

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Add global exception handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map health check endpoint
app.MapHealthChecks("/api/health");

app.Run();
