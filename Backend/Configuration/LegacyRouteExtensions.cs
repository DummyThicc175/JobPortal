using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace JobPortal;

public static class LegacyRouteExtensions
{
    // Keep bookmarked URLs and existing form submissions working after splitting controllers.
    // These routes run after the normal routes so new links use the new controller names.
    public static IEndpointRouteBuilder MapLegacyFeatureRoutes(this IEndpointRouteBuilder endpoints)
    {
        var routes = new[]
        {
            (Pattern: "Home/JobDetails/{id?}", Area: "", Controller: "Job", Action: "Details"),
            (Pattern: "Home/ApplyJob", Area: "Candidate", Controller: "Application", Action: "Apply"),
            (Pattern: "Home/SaveJob", Area: "Candidate", Controller: "SavedJob", Action: "Save"),
            (Pattern: "Candidate/Profile/AppliedJobs", Area: "Candidate", Controller: "Application", Action: "Index"),
            (Pattern: "Candidate/Profile/SavedJobs", Area: "Candidate", Controller: "SavedJob", Action: "Index"),
            (Pattern: "Candidate/Profile/SelectTemplate", Area: "Candidate", Controller: "CV", Action: "SelectTemplate"),
            (Pattern: "Candidate/Profile/CreateCV", Area: "Candidate", Controller: "CV", Action: "Create"),
            (Pattern: "Candidate/Profile/AnalyzeCVByAi", Area: "Candidate", Controller: "CVReview", Action: "Analyze"),
            (Pattern: "Employer/Job/Candidates1", Area: "Employer", Controller: "Applicant", Action: "Index"),
            (Pattern: "Employer/Job/Candidates", Area: "Employer", Controller: "Applicant", Action: "ByJob"),
            (Pattern: "Employer/Job/Messages", Area: "Employer", Controller: "Chat", Action: "Index")
        };

        foreach (var route in routes)
        {
            endpoints.MapControllerRoute(
                name: $"legacy_{route.Pattern}",
                pattern: route.Pattern,
                defaults: new { area = route.Area, controller = route.Controller, action = route.Action });
        }

        return endpoints;
    }
}
