namespace AuthenticationMicroservice.Tests.Helpers;

using Microsoft.EntityFrameworkCore;
using Models;

public class DbContextHelper
{
    public static AuthenticationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new AuthenticationDbContext(options);
        return context;
    }
}