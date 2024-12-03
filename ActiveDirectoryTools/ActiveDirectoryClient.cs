using Newtonsoft.Json.Linq;
using System;
using System.DirectoryServices;
using System.Linq;

namespace SharedResources
{
    public class ActiveDirectoryClient : IDisposable
    {
        private string[] _defaultProperties = new[] { "sAMAccountName", "userPrincipalName", "distinguishedName", "objectGUID", "userAccountControl" };

        private DirectoryEntry _directoryEntry;

        public ActiveDirectoryClient(ActiveDirectorySettings settings)
        {
            _directoryEntry = new DirectoryEntry
            {
                Username = settings.Username,
                Password = settings.Password,
                Path = $"LDAP://{settings.Server}/{settings.Path}",
                AuthenticationType = AuthenticationTypes.Secure
            };
        }

        public DirectoryEntry GetGroup(string sAMAccountName)
        {
            using (DirectorySearcher adSearch = new DirectorySearcher(_directoryEntry)
            {
                SearchScope = SearchScope.Subtree,
                Filter = $"(&(objectClass=group)(sAMAccountName={sAMAccountName}))",
                PageSize = 1
            })
            {
                return adSearch.FindOne()?.GetDirectoryEntry();
            }
        }

        public SearchResultCollection GetUsers(string filter = null, string[] propertiesToLoad = null)
        {
            using (DirectorySearcher search = new DirectorySearcher(_directoryEntry)
            {
                Filter = filter.IsEmpty() ? "(objectClass=user)" : $"(&(objectClass=user)({filter}))",
                PageSize = 10000
            })
            {
                propertiesToLoad = propertiesToLoad?.Any() == true
                    ? propertiesToLoad.Union(_defaultProperties)?.ToArray()
                    : _defaultProperties;

                search.PropertiesToLoad.AddRange(propertiesToLoad);

                return search.FindAll();
            }
        }

        public DirectorySearcher NewUserSearcher(string filter = null, string[] propertiesToLoad = null)
        {
            DirectorySearcher search = new DirectorySearcher(_directoryEntry)
            {
                Filter = filter.IsEmpty() ? "(objectClass=user)" : $"(&(objectClass=user)({filter}))",
                PageSize = 10000
            };

            propertiesToLoad = propertiesToLoad?.Any() == true
                ? propertiesToLoad.Union(_defaultProperties)?.ToArray()
                : _defaultProperties;

            search.PropertiesToLoad.AddRange(propertiesToLoad);
            return search;
        }

        public JArray GetUsersInformation(string filter = null, string[] propertiesToLoad = null, string[] asArray = null)
        {
            using (DirectorySearcher search = new DirectorySearcher(_directoryEntry)
            {
                Filter = filter.IsEmpty() ? "(objectClass=user)" : $"(&(objectClass=user)({filter}))",
                PageSize = 10000
            })
            {
                propertiesToLoad = propertiesToLoad?.Any() == true
                    ? propertiesToLoad.Union(_defaultProperties)?.ToArray()
                    : _defaultProperties;

                search.PropertiesToLoad.AddRange(propertiesToLoad);

                using (var result = search.FindAll())
                {
                    return new JArray(result
                        ?.Cast<SearchResult>()
                        ?.Select(sr => GetUserInformation(sr, asArray))
                        ?.ToList()
                    );
                }
            }
        }

        public static JObject GetUserInformation(SearchResult user, string[] asArray = null)
        {
            var jObj = new JObject();
            foreach (string prop in user.Properties.PropertyNames)
            {
                jObj[prop] = prop.Is("objectguid")
                ? new Guid((byte[])user.Properties[prop][0])
                : user.Properties[prop].Count <= 0 ? null
                    : asArray?.Contains(prop) == true
                        ? (JToken)new JArray(user.Properties[prop])
                        : (JToken)new JValue(user.Properties[prop][0]);
            }
            var parsed = Enum.TryParse((user.Properties["useraccountcontrol"][0]).ToString(), out UserFlags flags);
            jObj["active"] = parsed && !((string)jObj["objectguid"]).IsEmpty() && !flags.HasFlag(UserFlags.AccountDisabled);
            return jObj;
        }

        public void Dispose()
        {
            ((IDisposable)_directoryEntry).Dispose();
        }
    }
}
