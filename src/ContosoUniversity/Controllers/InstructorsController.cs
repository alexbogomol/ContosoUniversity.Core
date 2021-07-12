namespace ContosoUniversity.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Departments;
    using Data.Departments.Models;

    using Domain.Contracts;
    
    using MediatR;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    using Services.Queries.Instructors;

    using ViewModels.Instructors;

    public class InstructorsController : Controller
    {
        private readonly ICoursesRepository _coursesRepository;
        private readonly DepartmentsContext _departmentsContext;
        private readonly IMediator _mediator;

        public InstructorsController(
            DepartmentsContext departmentsContext,
            ICoursesRepository coursesRepository,
            IMediator mediator)
        {
            _departmentsContext = departmentsContext;
            _coursesRepository = coursesRepository;
            _mediator = mediator;
        }

        public async Task<IActionResult> Index(QueryInstructorsIndex request)
        {
            return View(await _mediator.Send(request));
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var result = await _mediator.Send(new QueryInstructorDetails(id.Value));
            
            return result is not null
                ? View(result)
                : NotFound();
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