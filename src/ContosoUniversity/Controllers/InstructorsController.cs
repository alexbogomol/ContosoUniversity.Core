﻿namespace ContosoUniversity.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Departments;
    using Data.Departments.Models;

    using Domain.Contracts;
    using Domain.Student;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    using Services;

    using ViewModels;
    using ViewModels.Instructors;

    public class InstructorsController : Controller
    {
        private readonly ICoursesRepository _coursesRepository;
        private readonly DepartmentsContext _departmentsContext;
        private readonly IStudentsRepository _studentsRepository;

        public InstructorsController(
            DepartmentsContext departmentsContext,
            ICoursesRepository coursesRepository,
            IStudentsRepository studentsRepository)
        {
            _departmentsContext = departmentsContext;
            _coursesRepository = coursesRepository;
            _studentsRepository = studentsRepository;
        }

        public async Task<IActionResult> Index(Guid? id, Guid? courseExternalId)
        {
            var instructors = await _departmentsContext.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments)
                .OrderBy(i => i.LastName)
                .AsNoTracking()
                .ToListAsync();

            var courses = await _coursesRepository.GetAll();

            CrossContextBoundariesValidator.EnsureInstructorsReferenceTheExistingCourses(instructors, courses);

            var viewModel = new InstructorIndexViewModel
            {
                Instructors = instructors.Select(x =>
                {
                    var assignedCourseIds = x.CourseAssignments.Select(ca => ca.CourseExternalId).ToArray();

                    return new InstructorListItemViewModel
                    {
                        Id = x.ExternalId,
                        FirstName = x.FirstMidName,
                        LastName = x.LastName,
                        HireDate = x.HireDate,
                        Office = x.OfficeAssignment?.Location,
                        AssignedCourseIds = assignedCourseIds,
                        AssignedCourses = courses
                            .Where(c => assignedCourseIds.Contains(c.EntityId))
                            .Select(c => $"{c.Code} {c.Title}"),
                        RowClass = id is not null && id == x.ExternalId
                            ? "table-success"
                            : string.Empty
                    };
                }).ToArray()
            };

            if (id is not null)
            {
                var instructor = viewModel.Instructors.Single(i => i.Id == id.Value);
                var instructorCourseIds = instructor.AssignedCourseIds.ToHashSet();
                var departmentNames = await _departmentsContext.Departments
                    .Where(x => courses.Select(_ => _.DepartmentId).Contains(x.ExternalId))
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.ExternalId, x => x.Name);
                CrossContextBoundariesValidator.EnsureCoursesReferenceTheExistingDepartments(courses, departmentNames.Keys);
                viewModel.Courses = courses
                    .Where(x => instructorCourseIds.Contains(x.EntityId))
                    .Select(x => new CourseListItemViewModel
                    {
                        Id = x.EntityId,
                        CourseCode = x.Code,
                        Title = x.Title,
                        Department = departmentNames[x.DepartmentId],
                        RowClass = courseExternalId is not null && courseExternalId == x.EntityId
                            ? "table-success"
                            : string.Empty
                    }).ToList();
            }

            if (courseExternalId is not null)
            {
                var students = await _studentsRepository.GetStudentsEnrolledForCourses(new[]
                {
                    courseExternalId.Value
                });
                
                CrossContextBoundariesValidator.EnsureEnrollmentsReferenceTheExistingCourses(
                    students.SelectMany(x => x.Enrollments).Distinct(), 
                    courses);
                
                viewModel.Students = students.Select(x => new EnrolledStudentViewModel
                {
                    StudentFullName = x.FullName(),
                    EnrollmentGrade = x.Enrollments[courseExternalId.Value].Grade.ToDisplayString()
                });
            }

            return View(viewModel);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _departmentsContext.Instructors.FirstOrDefaultAsync(m => m.ExternalId == id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(new InstructorDetailsViewModel
            {
                LastName = instructor.LastName,
                FirstName = instructor.FirstMidName,
                HireDate = instructor.HireDate,
                ExternalId = instructor.ExternalId
            });
        }

        public async Task<IActionResult> Create()
        {
            return View(new InstructorCreateForm
            {
                HireDate = DateTime.Now,
                AssignedCourses = await CreateAssignedCourseData()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InstructorCreateForm form)
        {
            if (form is null || ModelState.IsValid is false)
            {
                // form.AssignedCourses = CreateAssignedCourseData(instructor);
                return View(form);
            }

            var instructor = new Instructor
            {
                ExternalId = Guid.NewGuid(),
                FirstMidName = form.FirstName,
                LastName = form.LastName,
                HireDate = form.HireDate,
                OfficeAssignment = form.HasAssignedOffice
                    ? new OfficeAssignment {Location = form.Location}
                    : null
            };

            instructor.CourseAssignments = form.SelectedCourses?.Select(x => new CourseAssignment
            {
                InstructorId = instructor.Id, // not yet generated ???
                CourseExternalId = Guid.Parse(x)
            }).ToList();

            _departmentsContext.Add(instructor);
            await _departmentsContext.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id is null)
            {
                return NotFound();
            }

            var instructor = await _departmentsContext.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ExternalId == id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(new InstructorEditForm
            {
                ExternalId = instructor.ExternalId,
                LastName = instructor.LastName,
                FirstName = instructor.FirstMidName,
                HireDate = instructor.HireDate,
                Location = instructor.OfficeAssignment?.Location,
                AssignedCourses = await CreateAssignedCourseData(instructor)
            });
        }

        private async Task<AssignedCourseOption[]> CreateAssignedCourseData(Instructor instructor = null)
        {
            var allCourses = await _coursesRepository.GetAll();
            var instructorCourses = instructor?.CourseAssignments
                .Select(c => c.CourseExternalId) ?? Array.Empty<Guid>();

            return allCourses.Select(course => new AssignedCourseOption
            {
                CourseCode = course.Code,
                CourseExternalId = course.EntityId,
                Title = course.Title,
                Assigned = instructorCourses.Contains(course.EntityId)
            }).ToArray();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InstructorEditForm form)
        {
            if (form is null || ModelState.IsValid is false)
            {
                return BadRequest();
            }

            var instructor = await _departmentsContext.Instructors
                .Include(i => i.OfficeAssignment)
                .Include(i => i.CourseAssignments)
                .FirstOrDefaultAsync(m => m.ExternalId == form.ExternalId);
            if (instructor is null)
            {
                return NotFound();
            }

            instructor.FirstMidName = form.FirstName;
            instructor.LastName = form.LastName;
            instructor.HireDate = form.HireDate;
            instructor.OfficeAssignment = form.HasAssignedOffice
                ? new OfficeAssignment {Location = form.Location}
                : null;

            await UpdateInstructorCourses(form.SelectedCourses, instructor);

            try
            {
                await _departmentsContext.SaveChangesAsync();
            }
            catch (DbUpdateException /* ex */)
            {
                //Log the error (uncomment ex variable name and write a log.)
                ModelState.AddModelError("", "Unable to save changes. " +
                                             "Try again, and if the problem persists, " +
                                             "see your system administrator.");

                return View(new InstructorEditForm
                {
                    ExternalId = instructor.ExternalId,
                    LastName = instructor.LastName,
                    FirstName = instructor.FirstMidName,
                    HireDate = instructor.HireDate,
                    Location = instructor.OfficeAssignment?.Location,
                    AssignedCourses = await CreateAssignedCourseData(instructor)
                });
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task UpdateInstructorCourses(string[] selectedCourses, Instructor instructorToUpdate)
        {
            if (selectedCourses == null)
            {
                instructorToUpdate.CourseAssignments = new List<CourseAssignment>();
                return;
            }

            var selectedCoursesHs = new HashSet<string>(selectedCourses);
            var instructorCourses = new HashSet<Guid>
                (instructorToUpdate.CourseAssignments.Select(c => c.CourseExternalId));
            var courses = await _coursesRepository.GetAll();
            foreach (var course in courses)
                if (selectedCoursesHs.Contains(course.EntityId.ToString()))
                {
                    if (!instructorCourses.Contains(course.EntityId))
                        instructorToUpdate.CourseAssignments.Add(new CourseAssignment
                        {
                            InstructorId = instructorToUpdate.Id,
                            CourseExternalId = course.EntityId
                        });
                }
                else
                {
                    if (instructorCourses.Contains(course.EntityId))
                    {
                        var courseToRemove = instructorToUpdate
                            .CourseAssignments
                            .FirstOrDefault(i => i.CourseExternalId == course.EntityId);
                        
                        if (courseToRemove != null)
                            _departmentsContext.Remove(courseToRemove);
                    }
                }
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var instructor = await _departmentsContext.Instructors
                .FirstOrDefaultAsync(m => m.ExternalId == id);
            if (instructor == null)
            {
                return NotFound();
            }

            return View(new InstructorDetailsViewModel
            {
                LastName = instructor.LastName,
                FirstName = instructor.FirstMidName,
                HireDate = instructor.HireDate,
                ExternalId = instructor.ExternalId
            });
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var instructor = await _departmentsContext.Instructors
                .Include(i => i.CourseAssignments)
                .SingleAsync(i => i.ExternalId == id);

            var departments = await _departmentsContext.Departments
                .Where(d => d.InstructorId == instructor.Id)
                .ToListAsync();

            departments.ForEach(d => d.InstructorId = null);

            _departmentsContext.Instructors.Remove(instructor);

            await _departmentsContext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}