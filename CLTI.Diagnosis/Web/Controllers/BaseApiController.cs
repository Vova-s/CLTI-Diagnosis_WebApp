using Microsoft.AspNetCore.Mvc;

namespace CLTI.Diagnosis.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    // Base controller for API endpoints
    // MediatR support can be added later if needed
}
