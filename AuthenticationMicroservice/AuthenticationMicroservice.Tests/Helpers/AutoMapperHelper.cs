namespace AuthenticationMicroservice.Tests.Helpers;

using AutoMapper;

public class AutoMapperHelper
{
    public static IMapper CreateTestMapper()
    {
        var configuration = new MapperConfiguration(cfg => { cfg.AddProfile<AutoMapperProfile>(); });

        return configuration.CreateMapper();
    }
}