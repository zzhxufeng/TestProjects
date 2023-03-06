using System.Data.Entity;

namespace EFTest.OneToMany
{
    [TestClass]
    public class OneToManyTest
    {
        [TestMethod]
        public void OneToMany()
        {
            using (var ctx = new SchoolContext())
            {
                var stu1 = new Student()
                {
                    Name = "A",
                    Age = 1,
                };
                var stu2 = new Student()
                {
                    Name = "B",
                    Age = 2,
                };
                var stu3 = new Student()
                {
                    Name = "C",
                    Age = 3,
                };

                var grade = new Grade()
                {
                    Level = 1,
                    /*不显式指定这个集合的话, Student表的外键Grade_Id是空的.*/
                    Students = new List<Student>() { stu1, stu2, stu3 }
                };

                ctx.Students.Add(stu1);
                ctx.Students.Add(stu2);
                ctx.Students.Add(stu3);
                
                ctx.Grades.Add(grade);

                ctx.SaveChanges();
            }
        }
    }

    public class Student
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class Grade
    {
        public int Id { get; set; }
        public int Level { get; set; }

        public ICollection<Student> Students { get; set; }
    }

    public class SchoolContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<Grade> Grades { get; set; }
    }
}
