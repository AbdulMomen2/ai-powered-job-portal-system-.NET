using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;
using MercorClone.Web.Models.Entities;
using MercorClone.Web.Models.Dtos;
using MercorClone.Web.Repositories;

namespace MercorClone.Web.Controllers.Api
{
    [ApiController][Route("api/v1/companies")]
    [Produces(MediaTypeNames.Application.Json)]
    public class CompaniesApiController : ControllerBase
    {
        private readonly ICompanyRepository _companyRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CompaniesApiController> _logger;

        public CompaniesApiController(
            ICompanyRepository companyRepo, 
            UserManager<ApplicationUser> userManager,
            ILogger<CompaniesApiController> logger)
        {
            // Defensive programming: fail fast if dependencies are missing
            _companyRepo = companyRepo ?? throw new ArgumentNullException(nameof(companyRepo));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ────────────────────────────────────────────────────
        // GET /v1/companies/{id} 
        // ─────────────────────────────────────────────────────────────
        [HttpGet("{id:guid}")][ProducesResponseType(typeof(CompanyProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompany(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Fetching public profile for company {CompanyId}", id);

                var profile = await _companyRepo.GetCompanyProfileAsync(id, cancellationToken);
                
                if (profile == null)
                {
                    _logger.LogWarning("Company {CompanyId} not found.", id);
                    return Problem(
                        title: "Company Not Found",
                        detail: "The requested company does not exist or has been removed.",
                        statusCode: StatusCodes.Status404NotFound
                    );
                }

                return Ok(profile);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Request to fetch company {CompanyId} was canceled by the client.", id);
                return StatusCode(499); // Client Closed Request
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching company {CompanyId}", id);
                return Problem(
                    title: "Internal Server Error",
                    detail: "An unexpected error occurred while processing your request.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        // ─────────────────────────────────────────────────────────────
        // PUT /v1/companies/{id} 
        // ─────────────────────────────────────────────────────────────
        [HttpPut("{id:guid}")]
        [Authorize][ProducesResponseType(StatusCodes.Status200OK)][ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)][ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)][ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)][ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCompany(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("Unauthorized attempt to update company {CompanyId}.", id);
                    return Unauthorized();
                }

                _logger.LogInformation("User {UserId} attempting to update company {CompanyId}", user.Id, id);

                var result = await _companyRepo.UpdateCompanyAsync(id, user.Id, request, cancellationToken);

                return result switch
                {
                    RepoOperationStatus.NotFound => Problem(
                        title: "Company Not Found",
                        detail: "The requested company could not be found.",
                        statusCode: StatusCodes.Status404NotFound),

                    RepoOperationStatus.Forbidden => Problem(
                        title: "Forbidden",
                        detail: "You do not have administrative access to edit this company.",
                        statusCode: StatusCodes.Status403Forbidden),

                    RepoOperationStatus.Success => Ok(new { Message = "Company updated successfully." }),
                    
                    _ => Problem(statusCode: StatusCodes.Status400BadRequest)
                };
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {CompanyId}", id);
                return Problem(
                    title: "Update Failed",
                    detail: "An unexpected error occurred while saving the company data.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GET /v1/companies/{id}/jobs 
        // ─────────────────────────────────────────────────────────────
        [HttpGet("{id:guid}/jobs")][ProducesResponseType(typeof(PagedResponse<PublicJobListingDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompanyJobs(Guid id, [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                // Strict limits to prevent Denial of Service (DoS) via massive pagination requests
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 50);

                var response = await _companyRepo.GetCompanyJobsAsync(id, page, pageSize, cancellationToken);
                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching jobs for company {CompanyId} (Page: {Page})", id, page);
                return Problem(
                    title: "Fetch Failed",
                    detail: "Unable to retrieve job listings at this time.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        // ─────────────────────────────────────────────────────────────
        // GET /v1/companies/{id}/reviews
        // ─────────────────────────────────────────────────────────────
        [HttpGet("{id:guid}/reviews")][ProducesResponseType(typeof(PagedResponse<ReviewDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompanyReviews(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 50);

                var response = await _companyRepo.GetCompanyReviewsAsync(id, page, pageSize, cancellationToken);
                return Ok(response);
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reviews for company {CompanyId}", id);
                return Problem(
                    title: "Fetch Failed",
                    detail: "Unable to retrieve company reviews at this time.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }

        // ─────────────────────────────────────────────────────────────
        // POST /v1/companies/{id}/reviews
        // ─────────────────────────────────────────────────────────────

        [HttpPost("{id:guid}/reviews")][Authorize] 
        [ProducesResponseType(StatusCodes.Status201Created)][ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)][ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)][ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SubmitReview(Guid id, [FromBody] CreateReviewRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized();

                _logger.LogInformation("User {UserId} submitting review for company {CompanyId}", user.Id, id);

                var result = await _companyRepo.SubmitReviewAsync(id, user.Id, request, cancellationToken);

                return result switch
                {
                    RepoOperationStatus.NotFound => Problem(
                        title: "Company Not Found",
                        detail: "You are trying to review a company that does not exist.",
                        statusCode: StatusCodes.Status404NotFound),

                    RepoOperationStatus.Conflict => Problem(
                        title: "Review Already Exists",
                        detail: "You have already submitted a review for this company.",
                        statusCode: StatusCodes.Status409Conflict),

                    RepoOperationStatus.Success => CreatedAtAction(
                        nameof(GetCompanyReviews), 
                        new { id = id }, 
                        new { Message = "Review submitted successfully." }),

                    _ => Problem(statusCode: StatusCodes.Status400BadRequest)
                };
            }
            catch (OperationCanceledException)
            {
                return StatusCode(499);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit review for company {CompanyId} by user {UserId}", id, User.Identity?.Name);
                return Problem(
                    title: "Submission Failed",
                    detail: "An unexpected error occurred while saving your review.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        }
    }
}