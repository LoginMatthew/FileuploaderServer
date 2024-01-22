using FileUploaderServer.Models;
using Microsoft.EntityFrameworkCore;

namespace FileUploaderServer.Context
{
    public class DataFileDbContext: DbContext
    {
        public DbSet<DataFile>? DataFiles { get; set; }

        public DataFileDbContext(DbContextOptions<DataFileDbContext> options) :base(options)
        {

        }
    }
}