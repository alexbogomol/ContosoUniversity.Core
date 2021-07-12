namespace ContosoUniversity.Services.Handlers.Instructors
{
    using System.Threading;
    using System.Threading.Tasks;

    using Data.Departments;

    using Domain.Contracts.Exceptions;

    using MediatR;

    using Microsoft.EntityFrameworkCore;

    using Queries.Instructors;

    using ViewModels.Instructors;

    public class QueryInstructorDetailsHandler : IRequestHandler<QueryInstructorDetails, InstructorDetailsViewModel>
    {
        private readonly DepartmentsContext _departmentsContext;

        public QueryInstructorDetailsHandler(DepartmentsContext departmentsContext)
        {
            _departmentsContext = departmentsContext;
        }
        
        public async Task<InstructorDetailsViewModel> Handle(QueryInstructorDetails request, CancellationToken cancellationToken)
        {
            var instructor = await _departmentsContext.Instructors.FirstOrDefaultAsync(m => m.ExternalId == request.Id, cancellationToken);
            if (instructor == null)
                throw new EntityNotFoundException(nameof(instructor), request.Id);

            return new InstructorDetailsViewModel
            {
                LastName = instructor.LastName,
                FirstName = instructor.FirstMidName,
                HireDate = instructor.HireDate,
                ExternalId = instructor.ExternalId
            };
        }
    }
}