using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace EFTest.OneToOne
{
    [TestClass]
    public class OneToOneTest
    {
        [TestMethod] public void TestStudentIDCard()
        {
            using(var ctx = new SchoolContext())
            {
                var stu = new Student()
                {
                    Name = "Test",
                    Age = 1
                };

                var card = new IDCard()
                {
                    CardNo = "11213123"
                };
                ctx.IDCards.Add(card);
                ctx.Students.Add(stu);
                ctx.SaveChanges();
            }
        }

        [TestMethod] public void TestHusbandWife()
        {
            using (var ctx = new SocietyContext())
            {
                ctx.Husbands.Add(new Husband() { Name = "HHH" });
                ctx.Wifes.Add(new Wife() { Name = "WWW" });
                ctx.SaveChanges();
            }
        }

        [TestMethod] public void TestCountryCapital()
        {
            using (var ctx = new RegionContext())
            {
                var country = new Country()
                {
                    CountryName = "Test",
                };

                var capital = new Capital()
                {
                    CapitalName = "Capital Test",
                };

                ctx.Countries.Add(country);
                ctx.Capitals.Add(capital);
                ctx.SaveChanges();
            }
        }
    }

    public class Country
    {
        public int CountryId { get; set; }
        public string CountryName { get; set; }

        /*
         * 在EF6中, CapitalId无法生效, 也无法被配置:
         * https://learn.microsoft.com/en-us/ef/ef6/fundamentals/relationships;
         * 而在EF Core中, CapitalId可以生效, 也可以在context中被单独配置:
         * https://learn.microsoft.com/en-us/ef/core/modeling/relationships?tabs=fluent-api%2Cfluent-api-simple-key%2Csimple-key#other-relationship-patterns.
         */
        public int CapitalId { get; set; }
        public Capital Capital { get; set; }
    }

    public class Capital
    {
        public int CapitalId { get; set; }
        public string CapitalName { get; set; }

        public Country Country { get; set; }
    }

    public class RegionContext : DbContext 
    {
        public DbSet<Country> Countries { get; set; }
        public DbSet<Capital> Capitals { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Country>()
                .HasOptional(ct => ct.Capital)
                .WithRequired(cp => cp.Country);
        }
    }

    public class Wife
    {
        /*
         * 这种情况是在Wife表上创建了外键, 这个外键列是Id
         *   ALTER TABLE [dbo].[Wives]
         *      ADD CONSTRAINT [FK_dbo.Wives_dbo.Husbands_Id] FOREIGN KEY ([Id]) REFERENCES [dbo].[Husbands] ([Id]);
         */
        [ForeignKey("Husband")]
        public int Id { get; set; }
        public string Name { get; set; }

        public Husband Husband { get; set; }
    }

    public class Husband
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public Wife Wife { get; set; }
    }

    public class SocietyContext: DbContext
    {
        public DbSet<Wife> Wifes { get; set; }
        public DbSet<Husband> Husbands { get; set; }
    }

    public class Student
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public IDCard IDCard { get; set; }
    }

    public class IDCard
    {
        [ForeignKey("Student")]
        public int IDCardId { get; set; }
        public string CardNo { get; set; }
        public Student Student { get; set; }
    }

    public class SchoolContext: DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<IDCard> IDCards { get; set; }
    }
}