namespace ContosoUniversity.Services.Queries.Instructors
{
    using System;

    using MediatR;

    using ViewModels.Instructors;

    public record QueryInstructorDetails(Guid Id) : IRequest<InstructorDetailsViewModel>;
}