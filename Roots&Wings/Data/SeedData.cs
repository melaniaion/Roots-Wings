using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Roots_Wings.Models;

namespace Roots_Wings.Data
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Look for any moods already in the database.
                //if (!context.Moods.Any())
                //{
                //    context.Moods.AddRange(
                //        new Mood { Id = Guid.NewGuid().ToString(), Name = "Happy" },
                //        new Mood { Id = Guid.NewGuid().ToString(), Name = "Sad" },
                //        new Mood { Id = Guid.NewGuid().ToString(), Name = "Romantic" },
                //        new Mood { Id = Guid.NewGuid().ToString(), Name = "Chill" },
                //        new Mood { Id = Guid.NewGuid().ToString(), Name = "Party" },
                //        new Mood { Id = Guid.NewGuid().ToString(), Name = "Focus" },
                //        new Mood { Id = Guid.NewGuid().ToString(), Name = "Workout" }
                //    );

                //    context.SaveChanges();
                //}
                Console.WriteLine(context.Database.GetConnectionString());
                // ROLE SEEDING 
                if (!context.Roles.Any())
                {
                    Console.WriteLine("Seeding Roles");
                    context.Roles.AddRange(
                        new IdentityRole { Id = "48b9c598-8ba0-407c-9c32-d243f9d20a50", Name = "Admin", NormalizedName = "Admin".ToUpper() },
                        new IdentityRole { Id = "48b9c598-8ba0-407c-9c32-d243f9d20a52", Name = "User", NormalizedName = "User".ToUpper() }
                     );

                    var hasher = new PasswordHasher<ApplicationUser>();
                    //ADMIN
                    context.Users.AddRange(
                     new ApplicationUser
                     {
                         Id = "222dc744-48b7-4ed2-ab05-5bb119e4a0f6",
                         UserName = "admin@roots-wings.com",
                         EmailConfirmed = true,
                         NormalizedEmail = "ADMIN@ROOTS-WINGS.COM",
                         Email = "admin@roots-wings.com",
                         NormalizedUserName = "ADMIN@ROOTS-WINGS.COM",
                         PasswordHash = hasher.HashPassword(null, "Admin01!"),
                         Name = "Admin"
                         //ProfilePicture = "images/profile-pictures/AdminPicture.png"
                     });

                    //USERS
                    context.Users.AddRange(
                     new ApplicationUser
                     {
                         Id = "222dc744-48b7-4ed2-ab05-5bb119e4a0f0",
                         UserName = "maria@gmail.com",
                         EmailConfirmed = true,
                         NormalizedEmail = "MARIA@GMAIL.COM",
                         Email = "maria@gmail.com",
                         NormalizedUserName = "MARIA@GMAIL.COM",
                         PasswordHash = hasher.HashPassword(null, "Maria01!"),
                         Name = "Maria"
                         //ProfilePicture = "images/user-default-pictures/maria.png"
                     });
                    //context.Users.AddRange(
                    // new ApplicationUser
                    // {
                    //     Id = "222dc744-48b7-4ed2-ab05-5bb119e4a0f1",
                    //     UserName = "alma@gmail.com",
                    //     EmailConfirmed = true,
                    //     NormalizedEmail = "ALMA@GMAIL.COM",
                    //     Email = "alma@gmail.com",
                    //     NormalizedUserName = "ALMA@GMAIL.COM",
                    //     PasswordHash = hasher.HashPassword(null, "Alma01!"),
                    //     Name = "Alma",
                    //     ProfilePicture = "images/user-default-pictures/alma.png"
                    // });
                    //context.Users.AddRange(
                    // new ApplicationUser
                    // {
                    //     Id = "222dc744-48b7-4ed2-ab05-5bb119e4a0f2",
                    //     UserName = "ioana@gmail.com",
                    //     EmailConfirmed = true,
                    //     NormalizedEmail = "IOANA@GMAIL.COM",
                    //     Email = "ioana@gmail.com",
                    //     NormalizedUserName = "IOANA@GMAIL.COM",
                    //     PasswordHash = hasher.HashPassword(null, "Ioana01!"),
                    //     Name = "Ioana",
                    //     ProfilePicture = "images/user-default-pictures/ioana.png"
                    // });
                    //context.Users.AddRange(
                    // new ApplicationUser
                    // {
                    //     Id = "222dc744-48b7-4ed2-ab05-5bb119e4a0f3",
                    //     UserName = "eric@gmail.com",
                    //     EmailConfirmed = true,
                    //     NormalizedEmail = "ERIC@GMAIL.COM",
                    //     Email = "eric@gmail.com",
                    //     NormalizedUserName = "ERIC@GMAIL.COM",
                    //     PasswordHash = hasher.HashPassword(null, "Eric01!"),
                    //     Name = "Eric",
                    //     ProfilePicture = "images/user-default-pictures/eric.png"
                    // });
                    //context.Users.AddRange(
                    // new ApplicationUser
                    // {
                    //     Id = "222dc744-48b7-4ed2-ab05-5bb119e4a0f4",
                    //     UserName = "stefan@gmail.com",
                    //     EmailConfirmed = true,
                    //     NormalizedEmail = "STEFAN@GMAIL.COM",
                    //     Email = "stefan@gmail.com",
                    //     NormalizedUserName = "STEFAN@GMAIL.COM",
                    //     PasswordHash = hasher.HashPassword(null, "Stefan01!"),
                    //     Name = "Stefan",
                    //     ProfilePicture = "images/user-default-pictures/stefan.png"
                    // });
                    //context.Users.AddRange(
                    // new ApplicationUser
                    // {
                    //     Id = "222dc744-48b7-4ed2-ab05-5bb119e4a0f5",
                    //     UserName = "andrei@gmail.com",
                    //     EmailConfirmed = true,
                    //     NormalizedEmail = "ANDREI@GMAIL.COM",
                    //     Email = "andrei@gmail.com",
                    //     NormalizedUserName = "ANDREI@GMAIL.COM",
                    //     PasswordHash = hasher.HashPassword(null, "Andrei01!"),
                    //     Name = "Andrei",
                    //     ProfilePicture = "images/user-default-pictures/andrei.png"
                    // });
                    //context.Users.AddRange(
                    // new ApplicationUser
                    // {
                    //     Id = "222dc744-48b7-4ed2-ab05-5bb119e4a0f7",
                    //     UserName = "elena@gmail.com",
                    //     EmailConfirmed = true,
                    //     NormalizedEmail = "ELENA@GMAIL.COM",
                    //     Email = "elena@gmail.com",
                    //     NormalizedUserName = "ELENA@GMAIL.COM",
                    //     PasswordHash = hasher.HashPassword(null, "Elena01!"),
                    //     Name = "Elena",
                    //     ProfilePicture = "images/user-default-pictures/elena.png"
                    // });
                    //context.Users.AddRange(
                    // new ApplicationUser
                    // {
                    //     Id = "222dc744-48b7-4ed2-ab05-5bb119e4a0f8",
                    //     UserName = "liviu@gmail.com",
                    //     EmailConfirmed = true,
                    //     NormalizedEmail = "LIVIU@GMAIL.COM",
                    //     Email = "liviu@gmail.com",
                    //     NormalizedUserName = "LIVIU@GMAIL.COM",
                    //     PasswordHash = hasher.HashPassword(null, "Liviu01!"),
                    //     Name = "Liviu",
                    //     ProfilePicture = "images/user-default-pictures/liviu.png"
                    // });

                    //ADMIN
                    context.UserRoles.AddRange(
                         new IdentityUserRole<string>
                         {
                             RoleId = "48b9c598-8ba0-407c-9c32-d243f9d20a50",
                             UserId = "222dc744-48b7-4ed2-ab05-5bb119e4a0f6"
                         });

                    //USERS
                    context.UserRoles.AddRange(
                         new IdentityUserRole<string>
                         {
                             RoleId = "48b9c598-8ba0-407c-9c32-d243f9d20a52",
                             UserId = "222dc744-48b7-4ed2-ab05-5bb119e4a0f0"
                         });
                    //context.UserRoles.AddRange(
                    //     new IdentityUserRole<string>
                    //     {
                    //         RoleId = "48b9c598-8ba0-407c-9c32-d243f9d20a52",
                    //         UserId = "222dc744-48b7-4ed2-ab05-5bb119e4a0f1"
                    //     });
                    //context.UserRoles.AddRange(
                    //     new IdentityUserRole<string>
                    //     {
                    //         RoleId = "48b9c598-8ba0-407c-9c32-d243f9d20a52",
                    //         UserId = "222dc744-48b7-4ed2-ab05-5bb119e4a0f2"
                    //     });
                    //context.UserRoles.AddRange(
                    //     new IdentityUserRole<string>
                    //     {
                    //         RoleId = "48b9c598-8ba0-407c-9c32-d243f9d20a52",
                    //         UserId = "222dc744-48b7-4ed2-ab05-5bb119e4a0f3"
                    //     });
                    //context.UserRoles.AddRange(
                    //     new IdentityUserRole<string>
                    //     {
                    //         RoleId = "48b9c598-8ba0-407c-9c32-d243f9d20a52",
                    //         UserId = "222dc744-48b7-4ed2-ab05-5bb119e4a0f4"
                    //     });
                    //context.UserRoles.AddRange(
                    //     new IdentityUserRole<string>
                    //     {
                    //         RoleId = "48b9c598-8ba0-407c-9c32-d243f9d20a52",
                    //         UserId = "222dc744-48b7-4ed2-ab05-5bb119e4a0f5"
                    //     });
                    //context.UserRoles.AddRange(
                    //     new IdentityUserRole<string>
                    //     {
                    //         RoleId = "48b9c598-8ba0-407c-9c32-d243f9d20a52",
                    //         UserId = "222dc744-48b7-4ed2-ab05-5bb119e4a0f7"
                    //     });
                    //context.UserRoles.AddRange(
                    //     new IdentityUserRole<string>
                    //     {
                    //         RoleId = "48b9c598-8ba0-407c-9c32-d243f9d20a52",
                    //         UserId = "222dc744-48b7-4ed2-ab05-5bb119e4a0f8"
                    //     });

                    context.SaveChanges();
                }
            }
        }
    }
}
