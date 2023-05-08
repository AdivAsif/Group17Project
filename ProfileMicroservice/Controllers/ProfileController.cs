namespace Group17profile.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Middleware;
using Models.DTOs;
using Services;
using Swashbuckle.AspNetCore.Annotations;

[Route("api/[controller]")]
[ApiController]
public class ProfileController : DefaultAuthenticationController
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("GetProfileForUser")]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<ProfileDTO>))]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    public async Task<ActionResult<ResponseEnvelope<ProfileDTO>>> GetProfileForUser()
    {
        try
        {
            return Ok(await _profileService.GetProfileForUser(UserId));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("CreateOrUpdateProfile")]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<ProfileDTO>))]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    public async Task<ActionResult<ResponseEnvelope<ProfileDTO>>> CreateOrUpdateProfile([FromBody] ProfileDTO profile)
    {
        try
        {
            var newProfile = await _profileService.CreateOrUpdateProfile(profile, UserId);
            return Ok(newProfile);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("UploadProfilePicture")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ResponseEnvelope<ProfileDTO>>> UploadProfilePicture(IFormFile? profilePicture)
    {
        if (profilePicture == null)
            return BadRequest("Please upload a file.");
        return Ok(await _profileService.UploadProfilePicture(UserId, profilePicture));
    }

    [HttpPost("UploadBannerPicture")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ResponseEnvelope<ProfileDTO>>> UploadBannerPicture(IFormFile? bannerPicture)
    {
        if (bannerPicture == null)
            return BadRequest("Please upload a file.");
        return Ok(await _profileService.UploadBannerPicture(UserId, bannerPicture));
    }

    [HttpGet("GetProfileByUserId")]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<ProfileDTO>))]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseEnvelope<ProfileDTO>>> GetProfileByUserId(int userId)
    {
        try
        {
            return Ok(await _profileService.GetProfileForUser(userId));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("GetProfilesByUserIds")]
    [SwaggerResponse(200, Type = typeof(ResponseEnvelope<List<ProfileDTO>>))]
    [SwaggerResponse(400, Type = typeof(ResponseEnvelope<BadRequestObjectResult>))]
    [AllowAnonymous]
    public async Task<ActionResult<ResponseEnvelope<List<ProfileDTO>>>> GetProfilesByUserIds(List<int> userIds)
    {
        try
        {
            return Ok(await _profileService.GetProfilesByUserIds(userIds));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}