using Newtonsoft.Json;
using System;

namespace SharedResources
{
    public class Contact
    {
        [JsonProperty("id")]
        public int? ID { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("created")]
        public string CreatedLink { get; set; }

        [JsonProperty("duplicate")]
        public string DuplicateLink { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("suffix")]
        public string Suffix { get; set; }

        [JsonProperty("referred_to")]
        public string ReferredTo { get; set; }

        [JsonProperty("referred_by")]
        public string ReferredBy { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("email_work")]
        public string EmailWork { get; set; }

        [JsonProperty("address")]
        public string AddressLine1 { get; set; }

        [JsonProperty("address_2")]
        public string AddressLine2 { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("zip_code")]
        public string Zip { get; set; }

        [JsonProperty("phone_cell")]
        public string Phone_Cell { get; set; }

        [JsonProperty("phone_office")]
        public string Phone_Office { get; set; }

        [JsonProperty("phone_home")]
        public string Phone_Home { get; set; }

        [JsonProperty("fax")]
        public string Fax { get; set; }

        [JsonProperty("linkedin_url")]
        public string LinkedInUrl { get; set; }

        [JsonProperty("other_url")]
        public string OtherUrl { get; set; }

        [JsonProperty("employer_name")]
        public string EmployerName { get; set; }

        [JsonProperty("employer_address")]
        public string EmployerAddressLine1 { get; set; }

        [JsonProperty("employer_address_2")]
        public string EmployerAddressLine2 { get; set; }

        [JsonProperty("employer_city")]
        public string EmployerCity { get; set; }

        [JsonProperty("employer_state")]
        public string EmployerState { get; set; }

        [JsonProperty("employer_zip")]
        public string EmployerZip { get; set; }

        [JsonProperty("employer_license_number")]
        public string EmployerLicenseNumber { get; set; }

        [JsonProperty("license_number")]
        public string LicenseNumber { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("creation_date")]
        public DateTime? CreationDate { get; set; }

        [JsonProperty("last_contacted_date")]
        public DateTime? LastContactedDate { get; set; }

        [JsonProperty("internal_created_at")]
        public DateTime? InternalCreatedAt { get; set; }

        [JsonProperty("internal_updated_at")]
        public DateTime? InternalUpdatedAt { get; set; }

        [JsonProperty("last_modified_date")]
        public DateTime? LastModifiedDate { get; set; }

        [JsonProperty("pre_approval_issued_date")]
        public DateTime? PreApprovalIssuedDate { get; set; }

        [JsonProperty("list_date")]
        public DateTime? ListDate { get; set; }

        [JsonProperty("close_date")]
        public DateTime? CloseDate { get; set; }

        [JsonProperty("credit_score_date")]
        public DateTime? CreditScoreDate { get; set; }

        [JsonProperty("credit_score_expiration_date")]
        public DateTime? CreditScoreExpirationDate { get; set; }

        [JsonProperty("external_id")]
        public string ExternalId { get; set; }

        [JsonProperty("classification")]
        public string Classification { get; set; }

        [JsonProperty("credit_score")]
        public string CreditScore { get; set; }

        [JsonProperty("birthday")]
        public string Birthday { get; set; }

        [JsonProperty("website_url")]
        public string WebsiteURL { get; set; }

        [JsonProperty("ok_to_mail")]
        public int? OkToMail { get; set; }

        [JsonProperty("ok_to_email")]
        public int? OkToEmail { get; set; }

        [JsonProperty("ok_to_call")]
        public int? OkToCall { get; set; }

        [JsonProperty("preferences")]
        public Preferences Preferences { get; set; }

        [JsonProperty("owner")]
        public Owner Owner { get; set; }

        [JsonProperty("contact_groups")]
        public ContactGroup[] ContactGroups { get; set; }

        [JsonProperty("external_ids")]
        public ExternalIds[] ExternalIds { get; set; }

        [JsonProperty("external_status")]
        public ExternalStatus ExternalStatus { get; set; }
    }

    public class Preferences
    {
        [JsonProperty("is_silenced")]
        public bool? IsSilenced { get; set; }
    }

    public class Owner
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("external_id")]
        public string ExternalId { get; set; }

        [JsonProperty("id")]
        public long? Id { get; set; }
    }

    public class ExternalStatus
    {
        [JsonProperty("status_name")]
        public string Status { get; set; }
    }

    public class ContactGroup
    {
        [JsonProperty("group_name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class ExternalIds
    {
        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("external_id")]
        public string ExternalId { get; set; }
    }
}
