using Microsoft.AspNetCore.Mvc;
using TripTracker.SharedKernel.TripTracker.SharedKernel;

namespace TripTracker.SharedKernel.TripTracker.Framework;

[ApiController]
[Route("api/[controller]")]
public abstract class ApplicationController : ControllerBase
{
    public override OkObjectResult Ok(object? value)
    {
        var envelope = Envelope.Ok(value);

        return base.Ok(envelope);
    }
}
