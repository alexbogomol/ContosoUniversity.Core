﻿using ContosoUniversity.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoUniversity.Data
{
    public class SchoolContext : DbContext
    {
        public SchoolContext(DbContextOptions<SchoolContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<OfficeAssignment> OfficeAssignments { get; set; }
        public DbSet<CourseAssignment> CourseAssignments { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Course>().ToTable("Course", DbSchemas.Course);
            
            modelBuilder.Entity<Enrollment>().ToTable("Enrollment", DbSchemas.Student);
            modelBuilder.Entity<Student>().ToTable("Student", DbSchemas.Student);
            
            modelBuilder.Entity<Department>().ToTable("Department", DbSchemas.Department);
            modelBuilder.Entity<Instructor>().ToTable("Instructor", DbSchemas.Department);
            modelBuilder.Entity<OfficeAssignment>().ToTable("OfficeAssignment", DbSchemas.Department);
            modelBuilder.Entity<CourseAssignment>().ToTable("CourseAssignment", DbSchemas.Department)
                .HasKey(c => new { c.CourseUid, c.InstructorID });
        }

        private static class DbSchemas
        {
            public static string Course = "crs";
            public static string Department = "dpt";
            public static string Student = "std";
        }
    }
}