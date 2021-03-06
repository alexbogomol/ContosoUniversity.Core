namespace ContosoUniversity.Services.Handlers.Departments
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Data.Departments;

    using MediatR;

    using Microsoft.EntityFrameworkCore;

    using Queries.Departments;

    using ViewModels.Departments;

    public class QueryDepartmentsIndexHandler : IRequestHandler<QueryDepartmentsIndex, IList<DepartmentListItemViewModel>>
    {
        private readonly DepartmentsContext _departmentsContext;

        public QueryDepartmentsIndexHandler(DepartmentsContext departmentsContext)
        {
            _departmentsContext = departmentsContext;
        }
        
        public async Task<IList<DepartmentListItemViewModel>> Handle(QueryDepartmentsIndex request, CancellationToken cancellationToken)
        {
            var departments = await _departmentsContext.Departments
                .Include(d => d.Administrator)
                .ToListAsync(cancellationToken: cancellationToken);

            return departments.Select(x => new DepartmentListItemViewModel
            {
                Name = x.Name,
                Budget = x.Budget,
                StartDate = x.StartDate,
                Administrator = x.Administrator?.FullName,
                ExternalId = x.ExternalId
            }).ToList();
        }
    }
}