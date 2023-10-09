using Novell.Directory.Ldap;
using Microsoft.Extensions.Options;
using System;


namespace riskportal.Services
{
    public class LdapAuthenticationService
    {
        private readonly LdapSettings _ldapSettings;
   
        public LdapAuthenticationService(
            IOptions<LdapSettings> ldapSettings)
        {
            _ldapSettings = ldapSettings.Value;
        }

        public (bool, string) Authenticate(string username, string password)
        {
            using var connection = new LdapConnection();
            try
            {
                string fullBaseDN = _ldapSettings.BaseDN; // Get the full BaseDN

                connection.Connect(_ldapSettings.Server, _ldapSettings.Port);
                connection.Bind(_ldapSettings.Username, _ldapSettings.Password);

                // Construct the search filter
                string searchFilter = $"(mail={username})";

                // Perform an LDAP search to find the user's DN
                string userDn = FindUserDn(connection, fullBaseDN, searchFilter);

                if (userDn != null)
                {
                    // Attempt to bind with the user's DN and password
                    connection.Bind(userDn, password);

                    // Extract the CN value from the user's DN
                    string cn = GetCommonName(userDn);

                    return (connection.Bound, cn);
                }

                return (false, null);
            }
            catch (LdapException)
            {
                return (false, null);
            }
        }
        private static string GetCommonName(string userDn)
        {
            if (userDn != null)
            {
                // Parse the user's DN to extract the CN
                var dnComponents = userDn.Split(',');
                foreach (var component in dnComponents)
                {
                    if (component.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                    {
                        return component[3..];
                    }
                }
            }

            return string.Empty;
        }
    

    public (bool, string) AuthenticateAdmin(string username, string password)
        {
            using var connection = new LdapConnection();
            try
            {
                string fullBaseDN = _ldapSettings.BaseDN; // Get the full BaseDN

                connection.Connect(_ldapSettings.Server, _ldapSettings.Port);
                connection.Bind(_ldapSettings.Username, _ldapSettings.Password);

                // Construct the search filter
                string searchFilter = $"(mail={username})";

                // Perform an LDAP search to find the user's DN
                string userDn = FindAdminDn(connection, fullBaseDN, searchFilter);

                if (userDn != null)
                {
                    // Attempt to bind with the user's DN and password
                    connection.Bind(userDn, password);

                    // Extract the CN value from the user's DN
                    string cn = GetCommonName(userDn);

                    return (connection.Bound, cn);
                }

                return (false, null);
            }
            catch (LdapException)
            {
                return (false, null);
            }
        }
        private static string FindAdminDn(LdapConnection connection, string fullBaseDN, string searchFilter)
        {
            try
            {
                // Specify the search controls
                LdapSearchConstraints searchConstraints = new();
                searchConstraints.ReferralFollowing = true; // Allow following referrals

                // Perform an LDAP search to find the user's DN
                // Perform the search under "OU=Risk Management"
                string adminBaseDN = "OU=Risk Management," + fullBaseDN;
                ILdapSearchResults searchResults = connection.Search(
                    adminBaseDN,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    null, // Attributes to retrieve (null means all)
                    false, // Don't return only attributes
                    searchConstraints // Apply search constraints
                );
                if (searchResults.HasMore())
                {
                    var entry = searchResults.Next();
                    return entry.Dn;
                }
            }
            finally
            {
                // No need to call Dispose for ILdapSearchResults
            }

            return null;
        }


        private static string FindUserDn(LdapConnection connection, string fullBaseDN, string searchFilter)
        {
            try
            {
                // Specify the search controls
                LdapSearchConstraints searchConstraints = new();
                searchConstraints.ReferralFollowing = true; // Allow following referrals

                // Perform an LDAP search to find the user's DN
                // Perform the search
                ILdapSearchResults searchResults = connection.Search(
                    fullBaseDN,
                    LdapConnection.ScopeSub,
                    searchFilter,
                    null, // Attributes to retrieve (null means all)
                    false, // Don't return only attributes
                    searchConstraints // Apply search constraints
                );
                if (searchResults.HasMore())
                {
                    var entry = searchResults.Next();
                    return entry.Dn;
                }
            }
            finally
            {
                // No need to call Dispose for ILdapSearchResults
            }

            return null;
        }
    }
}