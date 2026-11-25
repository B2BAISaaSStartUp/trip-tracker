using TripTracker.Api.Cors;
using TripTracker.Api.Hooks;
using TripTracker.Identity;
using TripTracker.Photos;
using TripTracker.SharedKernel.TripTracker.Core;
using TripTracker.SharedKernel.TripTracker.Framework;
using TripTracker.SharedKernel.TripTracker.Framework.EndpointResults;
using TripTracker.SharedKernel.TripTracker.Framework.Middlewares;
using TripTracker.Trips;
using TripTracker.Trips.Contracts;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = context =>
            ModelStateToEnvelopeMapper.ToBadRequest(context.ModelState));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddAuthorization();
builder.Services.AddTripsModule(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddPhotosModule(builder.Configuration);
builder.Services.AddWebClientCors(builder.Configuration);

// Deleting a trip also deletes its photos (FR-07).
builder.Services.AddScoped<ITripDeletionHook, PhotoTripDeletionHook>();

var app = builder.Build();

app.UseExceptionMiddleware();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Before HTTPS redirection and authentication: a preflight request must not be redirected and carries no token,
// and 401 responses need the CORS headers too, or the web client cannot read them.
app.UseCors(WebClientCors.PolicyName);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
