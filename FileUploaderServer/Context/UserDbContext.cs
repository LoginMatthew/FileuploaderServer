using FileUploaderServer.Models;
using Microsoft.EntityFrameworkCore;

namespace FileUploaderServer.Context
{
    public class UserDbContext : DbContext
    {
        public DbSet<UserModel>? Users { get; set; }
        public UserDbContext(DbContextOptions<UserDbContext> options) :base(options)
        {

        }
    }
}