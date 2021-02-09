﻿namespace ContosoUniversity.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Data;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    using Models;

    using ViewModels;
    using ViewModels.Students;

    public class StudentsController : Controller
    {
        private readonly SchoolContext _context;

        public StudentsController(SchoolContext context)
        {
            _context = context;
        }

        // GET: Students
        public async Task<IActionResult> Index(
            string sortOrder,
            string currentFilter,
            string searchString,
            int? pageNumber)
        {
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            var students = from s in _context.Students select s;
            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(s => s.LastName.Contains(searchString)
                                               || s.FirstMidName.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    students = students.OrderByDescending(s => s.LastName);
                    break;
                case "Date":
                    students = students.OrderBy(s => s.EnrollmentDate);
                    break;
                case "date_desc":
                    students = students.OrderByDescending(s => s.EnrollmentDate);
                    break;
                default:
                    students = students.OrderBy(s => s.LastName);
                    break;
            }

            var page = await PaginatedList<Student, StudentListItemViewModel>.CreateAsync(
                students.AsNoTracking(),
                pageNumber ?? 1,
                3,
                s => new StudentListItemViewModel
                {
                    LastName = s.LastName,
                    FirstName = s.FirstMidName,
                    EnrollmentDate = s.EnrollmentDate,
                    ExternalId = s.ExternalId
                });

            return View(new StudentIndexViewModel
            {
                CurrentSort = sortOrder,
                NameSortParm = string.IsNullOrWhiteSpace(sortOrder) ? "name_desc" : string.Empty,
                DateSortParm = sortOrder == "Date" ? "date_desc" : "Date",
                CurrentFilter = searchString,
                Page = page
            });
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(s => s.Enrollments)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ExternalId == id);

            if (student == null)
            {
                return NotFound();
            }

            var courseIds = student.Enrollments.Select(x => x.CourseExternalId).ToArray();

            var courseTitles = await _context.Courses
                .Where(x => courseIds.Contains(x.ExternalId))
                .ToDictionaryAsync(x => x.ExternalId, x => x.Title);

            return View(new StudentDetailsViewModel
            {
                LastName = student.LastName,
                FirstMidName = student.FirstMidName,
                EnrollmentDate = student.EnrollmentDate,
                ExternalId = student.ExternalId,
                Enrollments = student.Enrollments.Select(x => new EnrollmentViewModel
                {
                    CourseTitle = courseTitles[x.CourseExternalId],
                    Grade = x.Grade?.ToString()
                }).ToArray()
            });
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View(new StudentCreateForm
            {
                EnrollmentDate = DateTime.Now
            });
        }

        // POST: Students/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StudentCreateForm form)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _context.Add(new Student
                    {
                        LastName = form.LastName,
                        FirstMidName = form.FirstName,
                        EnrollmentDate = form.EnrollmentDate,
                        ExternalId = Guid.NewGuid()
                    });
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateException /* ex */)
            {
                //Log the error (uncomment ex variable name and write a log.
                ModelState.AddModelError("", "Unable to save changes. " +
                                             "Try again, and if the problem persists " +
                                             "see your system administrator.");
            }

            return View(form);
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id is null)
            {
                return BadRequest();
            }

            var student = await _context.Students.SingleAsync(x => x.ExternalId == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(new StudentEditForm
            {
                ExternalId = student.ExternalId,
                LastName = student.LastName,
                FirstName = student.FirstMidName,
                EnrollmentDate = student.EnrollmentDate
            });
        }

        // POST: Students/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(StudentEditForm form)
        {
            if (form is null || !ModelState.IsValid)
            {
                return BadRequest();
            }

            var student = await _context.Students.SingleAsync(s => s.ExternalId == form.ExternalId);
            if (student is null)
            {
                return BadRequest();
            }

            student.LastName = form.LastName;
            student.FirstMidName = form.FirstName;
            student.EnrollmentDate = form.EnrollmentDate;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException /* ex */)
            {
                //Log the error (uncomment ex variable name and write a log.)
                ModelState.AddModelError("", "Unable to save changes. " +
                                             "Try again, and if the problem persists, " +
                                             "see your system administrator.");
            }

            return View(form);
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(Guid? id, bool? saveChangesError = false)
        {
            if (id is null)
            {
                return BadRequest();
            }

            var student = await _context.Students.AsNoTracking().SingleAsync(m => m.ExternalId == id);
            if (student is null)
            {
                return NotFound();
            }

            return View(new StudentDeleteViewModel
            {
                LastName = student.LastName,
                FirstMidName = student.FirstMidName,
                EnrollmentDate = student.EnrollmentDate,
                ExternalId = student.ExternalId,
                SaveChangesError = saveChangesError.GetValueOrDefault()
            });
        }

        // POST: Students/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var student = await _context.Students.SingleAsync(x => x.ExternalId == id);
            if (student == null)
            {
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException /* ex */)
            {
                //Log the error (uncomment ex variable name and write a log.)
                return RedirectToAction(nameof(Delete), new {id, saveChangesError = true});
            }
        }
    }
}