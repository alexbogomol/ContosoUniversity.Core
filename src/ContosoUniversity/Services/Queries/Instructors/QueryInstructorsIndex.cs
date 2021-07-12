namespace ContosoUniversity.Services.Queries.Instructors
{
    using System;

    using MediatR;

    using ViewModels.Instructors;

    public record QueryInstructorsIndex(Guid? Id, Guid? CourseExternalId) : IRequest<InstructorIndexViewModel>;
}