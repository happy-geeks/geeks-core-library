namespace GeeksCoreLibrary.Modules.Branches.Interfaces;

/// <summary>
/// Service for doing anything with branches. A branch is a copy of the Wiser database where people can make changes and test them without affecting production.
/// This service contains methods to use a branch on the website and similar things.
/// </summary>
public interface IBranchesService
{
    /// <summary>
    /// Gets the database to use for the application. This will check if a cookie exists and then return that value.
    /// </summary>
    /// <returns>The name of the database to use for the selected branch, or <see langword="null"/> if there is no cookie or an invalid cookie.</returns>
    string GetDatabaseNameFromCookie();

    /// <summary>
    /// Encrypts and saves the name of the database to a session cookie.
    /// </summary>
    void SaveDatabaseNameToCookie(string databaseName);
}