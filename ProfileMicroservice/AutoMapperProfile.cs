namespace Group17profile;

using AutoMapper;
using Models.DTOs;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Models.Entities.Profile, ProfileDTO>().ForMember(opt => opt.FavouriteShows, src => src.Ignore());
        CreateMap<ProfileDTO, Models.Entities.Profile>().ForMember(opt => opt.FavouriteShows, src => src.Ignore());
    }
}