namespace AuthenticationMicroservice.Services;

using System.Globalization;
using System.Net;
using AutoMapper;
using Exceptions;
using Helpers;
using Microsoft.EntityFrameworkCore;
using Models.DTOs;
using Models.Entities;
using Repositories;

public interface IUserService
{
    Task<User> CreateUser(UserRegisterRequestDTO request);
    Task<UserDTO> GetUserById(int userId);
    Task<List<UserDTO>> GetUsersById(List<int> userIds);
    List<UserDTO> GetListOfAllUsers();
}

public class UserService : IUserService
{
    private readonly IMapper _mapper;
    private readonly IBaseRepository<User> _userRepo;

    public UserService(IMapper mapper, IBaseRepository<User> userRepo)
    {
        _mapper = mapper;
        _userRepo = userRepo;
    }

    public async Task<User> CreateUser(UserRegisterRequestDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.EmailAddress) || string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.ConfirmPassword) ||
            string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.Surname))
            throw new AuthenticationException("Necessary information missing.", HttpStatusCode.Unauthorized);
        if (_userRepo.GetAll().AsNoTracking()
                .FirstOrDefault(u => string.Equals(u.EmailAddress, request.EmailAddress)) != null)
            throw new AuthenticationException("User with that email address already registered. Login instead.");
        SecurityHelper.EnsurePasswordComplexity(request.Password, request.ConfirmPassword);
        var newUser = new User
        {
            FirstName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(request.FirstName),
            Surname = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(request.Surname),
            EmailAddress = request.EmailAddress.ToLower(),
            UnconfirmedEmail = request.EmailAddress.ToLower(),
            EmailConfirmationToken = Guid.NewGuid(),
            Password = SecurityHelper.CreatePasswordHash(request.Password)
        };
        return await _userRepo.CreateAndSaveAsync(newUser);
    }

    public async Task<UserDTO> GetUserById(int userId)
    {
        return _mapper.Map<UserDTO>(_userRepo.GetByIdThrowIfNull(userId));
    }

    public async Task<List<UserDTO>> GetUsersById(List<int> userIds)
    {
        var users = _userRepo.GetAll().AsNoTracking().Where(u => userIds.Contains(u.Id));
        return await _mapper.ProjectTo<UserDTO>(users).ToListAsync();
    }

    public List<UserDTO> GetListOfAllUsers()
    {
        return _mapper.ProjectTo<UserDTO>(_userRepo.GetAll().AsNoTracking()).ToList();
    }
}