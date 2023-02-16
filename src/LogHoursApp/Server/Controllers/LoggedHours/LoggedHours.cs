using Dapper;
using LogHoursApp.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Identity.Web.Resource;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;

namespace LogHoursApp.Server.Controllers.LoggedHours
{
    [Authorize]
    [ApiController]
    [Route("api/logged-hours")]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    public class LoggedHoursController : ControllerBase
    {
        private readonly ILogger<LoggedHoursController> _logger;
        private readonly IMediator mediator;

        public LoggedHoursController(
            ILogger<LoggedHoursController> logger,
            IMediator mediator)
        {
            _logger = logger;
            this.mediator = mediator;
        }

        [HttpGet]
        public async Task<List<LoggedHour>> Get()
        {
            var id = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var loggedHours = await this.mediator.Send(new GetLoggedHoursByWorderId()
            {
                UserId = id
            });

            return await Task.FromResult(loggedHours);
        }

        [HttpPost]
        public async Task Post([FromBody] LoggedHour request)
        {
            var id = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = this.User.FindFirstValue(ClaimTypes.Name);
            await this.mediator.Send(new AddLoggedHourRequest
            {
                UserId = id,
                Date = request.Date,
                Description = request.Description,
                Hours = request.Hours,
                UserName = name
            });
        }

        [HttpGet("in-review")]
        public async Task<List<LoggedHourInReview>> GetInReview()
        {
            var id = this.User.FindFirstValue(ClaimTypes.NameIdentifier);

            var loggedHours = await this.mediator.Send(new GetLoggedHoursInReview());

            return await Task.FromResult(loggedHours);
        }

        [HttpPost("in-review")]
        public async Task MarkAsReview([FromBody] MarkAsReviewed request)
        {
            await this.mediator.Send(new MarkAsReviewedRequest
            {
                Id = request.Id
            });
        }

        [HttpPost("report")]
        public async Task<List<LoggedHourRecord>> GetReport([FromBody] GetLoggedHoursReport request)
        {
            var loggedHours = await this.mediator.Send(new GetLoggedHoursReportRequest()
            {
                WorkerId = request.WorkerId,
                DateFilter = request.DateFilter,
                FilterType = (FilterTypeEnum)request.FilterType
            });

            return await Task.FromResult(loggedHours);
        }

        [HttpGet("workers")]
        public async Task<List<Worker>> GetWorker()
        {
            var workers = await this.mediator.Send(new GetWorkersRequest());

            return await Task.FromResult(workers);
        }
    }

    public class GetLoggedHoursByWorderId : IRequest<List<LoggedHour>>
    {
        public string UserId { get; set; }
    }

    public class GetLoggedHoursByWorderIdHandler : IRequestHandler<GetLoggedHoursByWorderId, List<LoggedHour>>
    {
        private readonly IConfiguration configuration;

        public GetLoggedHoursByWorderIdHandler(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<List<LoggedHour>> Handle(GetLoggedHoursByWorderId request, CancellationToken cancellationToken)
        {
            using (SqlConnection connection = new SqlConnection(this.configuration["ConnectionString"]))
            {
                var enumerable = await connection.QueryAsync<LoggedHour>(@"
                    select
                    [UserId], [UserName], [Description], [Hours], [Date], Approved
                    from LoggedHours
                    where UserId = @userId
                    order by [Date] asc
                ",
                    new
                    {
                        userId = request.UserId
                    });

                return enumerable.ToList();
            }
        }
    }

    public class GetLoggedHoursInReview : IRequest<List<LoggedHourInReview>>
    {
        public string UserId { get; set; }
    }

    public class GetLoggedHoursInReviewHandler : IRequestHandler<GetLoggedHoursInReview, List<LoggedHourInReview>>
    {
        private readonly IConfiguration configuration;

        public GetLoggedHoursInReviewHandler(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<List<LoggedHourInReview>> Handle(GetLoggedHoursInReview request, CancellationToken cancellationToken)
        {
            using (SqlConnection connection = new SqlConnection(this.configuration["ConnectionString"]))
            {
                var enumerable = await connection.QueryAsync<LoggedHourInReview>(@"
                    select
                    Id, [UserId], [UserName], [Description], [Hours], [Date]
                    from LoggedHours
                    where Approved = 0
                    order by [Date] asc
                ",
                    new
                    {
                        userId = request.UserId
                    });

                return enumerable.ToList();
            }
        }
    }

    public class MarkAsReviewedRequest : IRequest<Unit>
    {
        public int Id { get; set; }
    }

    public class MarkAsReviewedHandler : IRequestHandler<MarkAsReviewedRequest, Unit>
    {
        private readonly IConfiguration configuration;

        public MarkAsReviewedHandler(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<Unit> Handle(MarkAsReviewedRequest request, CancellationToken cancellationToken)
        {
            using (SqlConnection connection = new SqlConnection(this.configuration["ConnectionString"]))
            {
                await connection.ExecuteAsync(@"
                    update LoggedHours set Approved = 1 where Id = @id
                ",
                    new
                    {
                        id = request.Id
                    });
            }

            return Unit.Value;
        }
    }

    public class AddLoggedHourRequest : IRequest
    {
        public DateTime Date { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        public int Hours { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
    }

    public class AddLoggedHourRequestHandler : IRequestHandler<AddLoggedHourRequest, Unit>
    {
        private readonly IConfiguration configuration;

        public AddLoggedHourRequestHandler(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public async Task<Unit> Handle(AddLoggedHourRequest request, CancellationToken cancellationToken)
        {
            using (SqlConnection connection = new SqlConnection(this.configuration["ConnectionString"]))
            {
                await connection.ExecuteAsync(@"
                    insert into LoggedHours
                    ([UserId], [UserName], [Description], [Hours], [Date])
                    values
                    (@userId, @userName, @description, @hours, @date)
                ",
                    new
                    {
                        date = request.Date,
                        description = request.Description,
                        hours = request.Hours,
                        userId = request.UserId,
                        userName = request.UserName
                    });
            }

            return Unit.Value;
        }
    }

    public class GetLoggedHoursReportRequest : IRequest<List<LoggedHourRecord>>
    {
        public string WorkerId { get; set; }
        public FilterTypeEnum FilterType { get; set; }
        public DateTime DateFilter { get; set; }
    }

    public class GetWorkersRequest : IRequest<List<Worker>>
    {
    }

    public class GetLoggedHoursReportRequestHandler : IRequestHandler<GetLoggedHoursReportRequest, List<LoggedHourRecord>>,
        IRequestHandler<GetWorkersRequest, List<Worker>>
    {
        private readonly IConfiguration configuration;

        public GetLoggedHoursReportRequestHandler(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task<List<LoggedHourRecord>> Handle(GetLoggedHoursReportRequest request, CancellationToken cancellationToken)
        {
            using (SqlConnection connection = new SqlConnection(this.configuration["ConnectionString"]))
            {
                var sql = $@"
                    set datefirst 1;
                    select
                    Id, [UserId], [UserName], [Description], [Hours], [Date], [Approved]
                    from LoggedHours
                    where UserId = @userId
                    {(request.FilterType == FilterTypeEnum.DayFilter ? " and CAST([Date] as date) = CAST(@dateFilter as date)" : "")}
                    {(request.FilterType == FilterTypeEnum.MonthFilter ? " and YEAR([Date]) = @year and MONTH([Date]) = @month" : "")}
                    order by [Date] asc
                ";
                var enumerable = await connection.QueryAsync<LoggedHourRecord>(sql,
                    new
                    {
                        userId = request.WorkerId,
                        dateFilter = request.DateFilter,
                        month = request.DateFilter.Month,
                        year = request.DateFilter.Year
                    }
                );

                if (request.FilterType == FilterTypeEnum.WeekFilter)
                {
                    return enumerable.Where(i => ISOWeek.GetWeekOfYear(i.Date) == ISOWeek.GetWeekOfYear(request.DateFilter)).ToList();
                }

                return enumerable.ToList();
            }
        }

        public async Task<List<Worker>> Handle(GetWorkersRequest request, CancellationToken cancellationToken)
        {
            using (SqlConnection connection = new SqlConnection(this.configuration["ConnectionString"]))
            {
                var enumerable = await connection.QueryAsync<Worker>(@"
                    select distinct
                    [UserId], [UserName]
                    from LoggedHours
                    order by [UserName]
                ");

                return enumerable.ToList();
            }
        }
    }
}
