namespace LogicFit.Shared;

public static class LogicFitApi
{
    public const string Version = "v1";
    public const string BasePath = "/api/v1";
}

public sealed record ApiMeta(string RequestId, string Version = LogicFitApi.Version);
public sealed record ApiResponse<T>(T Data, ApiMeta Meta);
public sealed record ApiCollectionMeta(string RequestId, int Page, int PageSize, int Total, bool HasNext, string Version = LogicFitApi.Version);
public sealed record ApiCollectionResponse<T>(IReadOnlyList<T> Data, ApiCollectionMeta Meta);
public sealed record ApiError(string Code, string Message, IReadOnlyList<ApiFieldError>? FieldErrors = null);
public sealed record ApiErrorResponse(ApiError Error, ApiMeta Meta);

public sealed record HealthData(string Status, string Service, string Environment, string Version);
public sealed record ReadinessData(string Status, string Service, string Database, string Version);
public sealed record VersionData(string Version, string ApiVersion, string Environment);
