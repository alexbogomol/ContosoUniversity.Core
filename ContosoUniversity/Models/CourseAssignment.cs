﻿namespace ContosoUniversity.Models
{
    using System;

    public class CourseAssignment
    {
        public int InstructorId { get; set; }
        public Guid CourseUid { get; set; }
    }
}