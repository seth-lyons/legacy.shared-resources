using EllieMae.Encompass.BusinessEnums;
using EllieMae.Encompass.BusinessObjects;
using EllieMae.Encompass.BusinessObjects.Loans;
using EllieMae.Encompass.BusinessObjects.Users;
using EllieMae.Encompass.Client;
using EllieMae.Encompass.Collections;
using EllieMae.Encompass.Licensing;
using EllieMae.Encompass.Query;
using EllieMae.Encompass.Reporting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedResources
{
    public static class EncompassSDKTools
    {

        #region User

        public static bool IsInGroup(this User user, string groupName) => user?.GetUserGroups().Cast<UserGroup>()?.Select(group => group.Name)
            ?.Contains(groupName, StringComparer.InvariantCultureIgnoreCase) ?? false;

        public static bool IsAdmin(this User user, bool requireSuper = false) =>
            requireSuper
            ? user.HasPersona(new[] { "Super Administrator" })
            : user.HasPersona(new[] { "Administrator", "Super Administrator" });

        public static bool HasPersona(this User user, string personaName) => user.HasPersona(new[] { personaName });

        public static bool HasPersona(this User user, IEnumerable<string> personaNames, bool mustHaveAll = false)
        {
            var userPersonas = user
                ?.Personas
                ?.Cast<Persona>()
                ?.Select(persona => persona?.Name);

            Func<string, bool> predicate = (string name) => userPersonas?.Contains(name, StringComparer.InvariantCultureIgnoreCase) == true;
            return mustHaveAll ? personaNames.All(predicate) : personaNames.Any(predicate);
        }

        public static T GetCDO<T>(this User user, string cdoName = null, bool emptyObjectOnError = false) where T : new()
        {
            var retrievedSettings = user.GetCustomDataObject(cdoName ?? $"{typeof(T).Name}.json")?.ToString(Encoding.UTF8);
            if (retrievedSettings == null)
                return new T();

            try
            {
                return JsonConvert.DeserializeObject<T>(retrievedSettings);
            }
            catch
            {
                if (emptyObjectOnError)
                    return new T();
                else
                    throw;
            }
        }

        public static JToken GetCDO(this User user, string cdoName, bool emptyObjectOnError = false)
        {
            var retrievedSettings = user.GetCustomDataObject(cdoName)?.ToString(Encoding.UTF8);
            if (retrievedSettings == null)
                return new JObject();
            try
            {
                return JToken.Parse(retrievedSettings);
            }
            catch
            {
                if (emptyObjectOnError)
                    return new JObject();
                else
                    throw;
            }
        }

        public static void SaveCDO<T>(this User user, T settings, string cdoName = null) =>
            user.SaveCustomDataObject(cdoName ?? $"{typeof(T).Name}.json", new DataObject(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(settings))));

        public static void SaveCDO(this User user, JToken cdo, string cdoName) =>
            user.SaveCustomDataObject(cdoName, new DataObject(Encoding.UTF8.GetBytes(cdo.ToString())));

        #endregion

        #region Loans

        public static string FormattedGuid(this Loan loan) => new Guid(loan.Guid).ToString("D");

        public static bool LockedByUser(this Loan loan, string userId) =>
            loan?.GetCurrentLocks()?.Cast<LoanLock>()?.Any(ll => ll.LockType.Equals(LockType.Edit) && ll.LockedBy.Is(userId)) == true;

        public static void SendLoanToMilestone(this Loan loan, Milestone milestone)
        {
            if (milestone?.Name != null)
                loan.Log.MilestoneEvents.GetEventForMilestone(milestone.Name).Completed = false;
        }

        public static T GetCDO<T>(this Loan loan, string cdoName = null, bool emptyObjectOnError = false) where T : new()
        {
            var retrievedSettings = loan.GetCustomDataObject(cdoName ?? $"{typeof(T).Name}.json")?.ToString(Encoding.UTF8);
            if (retrievedSettings == null)
                return new T();

            try
            {
                return JsonConvert.DeserializeObject<T>(retrievedSettings);
            }
            catch
            {
                if (emptyObjectOnError)
                    return new T();
                else
                    throw;
            }
        }

        public static JToken GetCDO(this Loan loan, string cdoName, bool emptyObjectOnError = false)
        {
            var retrievedSettings = loan.GetCustomDataObject(cdoName)?.ToString(Encoding.UTF8);
            if (retrievedSettings == null)
                return new JObject();

            try
            {
                return JToken.Parse(retrievedSettings);
            }
            catch
            {
                if (emptyObjectOnError)
                    return new JObject();
                else
                    throw;
            }
        }

        public static void SaveCDO<T>(this Loan loan, T settings, string cdoName = null) =>
            loan.SaveCustomDataObject(cdoName ?? $"{typeof(T).Name}.json", new DataObject(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(settings))));

        public static void SaveCDO(this Loan loan, JToken cdo, string cdoName) =>
            loan.SaveCustomDataObject(cdoName, new DataObject(Encoding.UTF8.GetBytes(cdo.ToString())));

        #endregion


        #region Session

        public static bool IsProd(this Session session) => session.ServerURI.Is("https://be11129522.ea.elliemae.net$BE11129522");

        public static void UpdateLoans(this Session session, IEnumerable<string> guids, string field, object value) =>
            session.UpdateLoans(guids, new Dictionary<string, object> { { field, value } });

        public static void UpdateLoans(this Session session, string guid, string field, object value) =>
            session.UpdateLoans(new[] { guid }, new Dictionary<string, object> { { field, value } });

        public static void UpdateLoans(this Session session, string guid, IEnumerable<KeyValuePair<string, object>> fields)
            => session.UpdateLoans(new[] { guid }, fields);

        public static void UpdateLoans(this Session session, IEnumerable<string> guids, IEnumerable<KeyValuePair<string, object>> fields)
        {
            if (guids?.Any() != true) return;
            var batch = new BatchUpdate(new StringList(guids.ToList()));
            fields.ForEach(field => batch.Fields.Add(field.Key, field.Value));
            session.Loans.SubmitBatchUpdate(batch);
        }


        public static void SaveGlobalCDO(this Session session, JToken cdo, string cdoName)
            => session.SaveGlobalCDO(Encoding.UTF8.GetBytes(cdo?.ToString()), cdoName);

        public static void SaveGlobalCDO(this Session session, string cdo, string cdoName)
           => session.SaveGlobalCDO(Encoding.UTF8.GetBytes(cdo), cdoName);

        public static void SaveGlobalCDO(this Session session, byte[] cdo, string cdoName)
            => session.DataExchange.SaveCustomDataObject(cdoName, new DataObject(cdo));

        public static JToken GetGlobalCDO(this Session session, string cdoName, bool emptyObjectOnError = false)
        {
            var retrievedSettings = session.DataExchange.GetCustomDataObject(cdoName)?.ToString(Encoding.UTF8);
            if (retrievedSettings == null)
                return new JObject();

            try
            {
                return JToken.Parse(retrievedSettings);
            }
            catch
            {
                if (emptyObjectOnError)
                    return new JObject();
                else
                    throw;
            }
        }

        public static T GetGlobalCDO<T>(this Session session, string cdoName = null, bool emptyObjectOnError = false) where T : new()
        {
            var retrievedSettings = session.DataExchange.GetCustomDataObject(cdoName ?? $"{typeof(T).Name}.json")?.ToString(Encoding.UTF8);
            if (retrievedSettings == null)
                return new T();

            try
            {
                return JsonConvert.DeserializeObject<T>(retrievedSettings);
            }
            catch
            {
                if (emptyObjectOnError)
                    return new T();
                else
                    throw;
            }
        }

        public static JArray GetLoansByLoanNumber(this Session session, IEnumerable<string> loanNumbers, string[] fields = null)
        {
            QueryCriterion loanQuery = null;
            foreach (var loanNumber in loanNumbers)
            {
                loanQuery = loanQuery == null
                    ? new StringFieldCriterion("Loan.LoanNumber", loanNumber, StringFieldMatchType.CaseInsensitive, true)
                    : loanQuery.Or(new StringFieldCriterion("Loan.LoanNumber", loanNumber, StringFieldMatchType.CaseInsensitive, true));
            }

            StringList include = new StringList(fields ?? new[] { "Loan.LoanNumber", "Loan.LoanFolder", "Loan.Guid" });
            using (LoanReportCursor cursor = session.Reports.OpenReportCursor(include, loanQuery))
            {
                return new JArray(cursor
                    ?.Cast<LoanReportData>()
                    ?.Select(loanData =>
                    {
                        var output = new JObject();
                        loanData?.GetFieldNames()?.Cast<string>()?.ForEach(field => output[field] = new JValue(loanData[field]));
                        return output;
                    }));
            }
        }

        public static (string ClientID, string ClientSecret) GetVaultCredentials(this Session session)
        {
            var settings = SecretCDOEncryption.GetSecretsGlobalCDO(session);
            return ((string)settings["VaultClientID"], (string)settings["VaultClientSecret"]);
        }


        #endregion



        public static void RegisterMachine(string licenseKey = null)
        {
            LicenseManager mngr = new LicenseManager();

            if (!mngr.ValidateLicense(false))
            {
                if (mngr.LicenseKeyExists())
                {
                    try
                    {
                        mngr.RefreshLicense();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("License refresh failed: " + e.Message);
                    }
                }
                else
                {
                    try
                    {
                        if (licenseKey != null)
                            mngr.GenerateLicense(licenseKey);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("License generation failed: " + e.Message);
                    }
                }
            }
        }

        public static class KeyVaultSettings
        {
            public static string ClientID = "1ad2ad68-fef1-43da-afbb-d6c2469ba689";
            public static string VaultURL = "https://encompassvariables.vault.azure.net/";
            public static string CertificateThumbprint = "76DC9C2B55B9AB051777B78B793FC9318088BB62";
        }
    }
}
