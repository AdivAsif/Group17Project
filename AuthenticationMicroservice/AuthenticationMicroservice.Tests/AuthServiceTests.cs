namespace AuthenticationMicroservice.Tests;

using AutoMapper;
using Helpers;
using Microsoft.Extensions.Options;
using Models.DTOs;
using Models.Entities;
using Moq;
using Repositories;
using Services;
using Settings;

public class AuthServiceTests
{
    private readonly IMapper _mapper;

    public AuthServiceTests()
    {
        _mapper = AutoMapperHelper.CreateTestMapper();
    }

    [Fact]
    public async Task TestRegisterUser()
    {
        var registerRequest = new UserRegisterRequestDTO
        {
            EmailAddress = "aa03980@surrey.ac.uk",
            Password = "Testtest1.",
            ConfirmPassword = "Testtest1.",
            FirstName = "Adiv",
            Surname = "Asif"
        };

        await using var context = DbContextHelper.CreateInMemoryDbContext();
        var userRepo = new BaseRepository<User>(context);

        var userService = new UserService(_mapper, userRepo);
        var createdUser = await userService.CreateUser(registerRequest);
        Assert.True(createdUser.EmailAddress == "aa03980@surrey.ac.uk");
    }
    
    [Fact]
    public async Task TestRegisterAndLoginUser()
    {
        var registerRequest = new UserRegisterRequestDTO
        {
            EmailAddress = "aa03980@surrey.ac.uk",
            Password = "Testtest1.",
            ConfirmPassword = "Testtest1.",
            FirstName = "Adiv",
            Surname = "Asif"
        };

        var loginRequest = new UserLoginRequestDTO
        {
            EmailAddress = "aa03980@surrey.ac.uk",
            Password = "Testtest1."
        };

        await using var context = DbContextHelper.CreateInMemoryDbContext();
        var userRepo = new BaseRepository<User>(context);

        var userService = new UserService(_mapper, userRepo);
        var createdUser = await userService.CreateUser(registerRequest);

        var mockEmailService = new Mock<IEmailService>();
        var mockRefreshTokenService = new Mock<IRefreshTokenService>();
        mockRefreshTokenService.Setup(r => r.CreateNewTokenForUser(It.IsAny<int>())).ReturnsAsync(new AccessToken{Token = "test"});
        mockRefreshTokenService.Setup(r => r.CreateNewTokenForUser(It.IsAny<User>())).Returns(new AccessToken{Token = "test"});
        mockRefreshTokenService.Setup(r => r.GenerateRefreshToken(It.IsAny<int>())).ReturnsAsync(new RefreshToken{Token = "test"});

        var frontendString = new FrontendStrings {BaseUrl = "test"};
        var mockOptions = new Mock<IOptions<FrontendStrings>>();
        mockOptions.Setup(s => s.Value).Returns(frontendString);
        
        var authService = new AuthService(context, mockEmailService.Object, mockRefreshTokenService.Object, mockOptions.Object);
        var login = await authService.Authenticate(loginRequest);
        
        Assert.True(login.UserId == createdUser.Id);
    }
}