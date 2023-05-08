namespace AuthenticationMicroservice;

using AutoMapper;
using Models.DTOs;
using Models.Entities;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<User, UserDTO>();
        CreateMap<UserDTO, User>();
    }
}