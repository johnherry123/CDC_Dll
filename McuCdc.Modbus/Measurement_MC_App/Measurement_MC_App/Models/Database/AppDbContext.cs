using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;



namespace Measurement_MC_App.Models.Database
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Model> Models => Set<Model>();
        public DbSet<VisionParam> VisionParams => Set<VisionParam>();
        public DbSet<PointParam> PointParams => Set<PointParam>();
        public DbSet<Logs> Logs => Set<Logs>();
        public DbSet<Connector> Connectors => Set<Connector>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

           
            mb.Entity<Model>()
              .HasOne(x => x.VisionParam)
              .WithOne(x => x.Model)
              .HasForeignKey<VisionParam>(x => x.ModelId)
              .OnDelete(DeleteBehavior.Cascade);

         
            mb.Entity<Model>()
              .HasOne(x => x.PointParam)
              .WithOne(x => x.Model)
              .HasForeignKey<PointParam>(x => x.ModelId)
              .OnDelete(DeleteBehavior.Cascade);

        
            mb.Entity<Model>()
              .HasOne(x => x.Logs)
              .WithOne(x => x.Model)
              .HasForeignKey<Logs>(x => x.ModelId)
              .OnDelete(DeleteBehavior.Cascade);

         
        }
    }


    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

     
            var cs = config.GetConnectionString("MySql");

      
            if (string.IsNullOrWhiteSpace(cs))
                cs = config["Db:ConnectionString"];

            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException(
                    "Không tìm thấy connection string. Hãy thêm ConnectionStrings:MySql hoặc Db:ConnectionString trong appsettings.json.");

            var opt = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs))
                .EnableDetailedErrors()
                .Options;

            return new AppDbContext(opt);
        }
    }
}
