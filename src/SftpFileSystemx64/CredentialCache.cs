namespace SftpFileSystem;

internal class CredentialCache
{
    #region Private Fields

    private readonly IList<Credentials> _credList = [];

    #endregion

    #region Private Methods

    private void RemoveCredentials (string host, string user)
    {
        var credentials = GetCredentials(host, user);
        if (credentials != null)
        {
            _credList.Remove(credentials);
        }
    }

    #endregion

    internal IList<string> GetUsersForHost (string host)
    {
        IList<string> result = [];

        foreach (var cred in _credList)
        {
            if (cred.Host.Equals(host, StringComparison.Ordinal))
            {
                result.Add(cred.UserName);
            }
        }

        return result;
    }

    internal Credentials GetCredentials (string host, string user)
    {
        foreach (var cred in _credList)
        {
            if (cred.Host.Equals(host, StringComparison.Ordinal) && cred.UserName.Equals(user, StringComparison.Ordinal))
            {
                return cred;
            }
        }

        return null;
    }

    internal void Add (Credentials cred)
    {
        RemoveCredentials(cred.Host, cred.UserName);
        _credList.Add(cred);
    }
}