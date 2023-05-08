namespace Group17profile.Controllers;

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Middleware;

[ApiController]
[Authorize]
public class DefaultController : Controller
{
    public OkObjectResult Ok<T>(T value)
    {
        return base.Ok(new ResponseEnvelope<T>(value));
    }

    public override OkResult Ok()
    {
        throw new Exception("please use Ok with data object. It's ok to pass null");
    }

    public override BadRequestResult BadRequest()
    {
        throw new Exception("please use Badrequest with data object. It's ok to pass null");
    }

    public override BadRequestObjectResult BadRequest(object error)
    {
        return base.BadRequest(new ResponseEnvelope<object>(new ErrorDetails {Data = error}, 400));
    }

    public override UnauthorizedObjectResult Unauthorized(object error)
    {
        return base.Unauthorized(new ResponseEnvelope<object>(new ErrorDetails {Data = error}, 400));
    }

    protected static PropertyInfo? Sort(string? sort, Type type)
    {
        if (string.IsNullOrEmpty(sort))
            return null;
        try
        {
            var propertyInfo = type.GetProperty(sort);
            return propertyInfo;
        }
        catch
        {
            return null;
        }
    }
}