namespace ProjectManagementSystem.Api.Authentication;

public static class EmployeeClaimTypes
{
    /// <summary>
    /// Id of the employee the account belongs to. Not the id of the account itself, which stays in the standard
    /// name identifier claim.
    /// </summary>
    public const string EmployeeId = "employee_id";
}
