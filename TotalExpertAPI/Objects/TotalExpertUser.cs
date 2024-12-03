using Newtonsoft.Json;
using System;

namespace SharedResources
{
    public class TotalExpertUser
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("external_id")]
        public string ExternalID { get; set; }

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("saml_subject_id")]
        public object SAML_ID { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("info")]
        public UserInformation Information { get; set; }

        [JsonProperty("teams")]
        public Team[] Teams { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("role")]
        public UserRole Role { get; set; }

        [JsonProperty("settings_marketing")]
        public MarketingSettings Settings_Marketing { get; set; }


        [JsonProperty("internal_created_at")]
        public string CreatedText
        {
            get => Created?.ToString();
            set { Created = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? Created { get; set; }


        [JsonProperty("internal_updated_at")]
        public string ModifiedText
        {
            get => Modified?.ToString();
            set { Modified = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? Modified { get; set; }


        [JsonProperty("last_login_date")]
        public string LastLoginText
        {
            get => LastLogin?.ToString();
            set { LastLogin = DateTime.TryParse(value, out DateTime d) ? (DateTime?)d : null; }
        }
        public DateTime? LastLogin { get; set; }
    }

    public class UserInformation
    {
        [JsonProperty("address")]
        public string AddressLine1 { get; set; }

        [JsonProperty("address_2")]
        public string AddressLine2 { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("company")]
        public string Company { get; set; }

        [JsonProperty("cost_center")]
        public string CostCenter { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("job_title")]
        public string JobTitle { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }

        [JsonProperty("location_id")]
        public string LocationID { get; set; }

        [JsonProperty("phone_cell")]
        public string Phone_Mobile { get; set; }

        [JsonProperty("phone_fax")]
        public string Phone_Fax { get; set; }

        [JsonProperty("phone_office")]
        public string Phone_Office { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("testimonial_url")]
        public string TestimonialURL { get; set; }

        [JsonProperty("timezone_name")]
        public string Timezone { get; set; }

        [JsonProperty("website")]
        public string Website { get; set; }

        [JsonProperty("zip_code")]
        public string Zip { get; set; }
    }

    public class UserRole
    {
        [JsonProperty("role_name")]
        public string Name { get; set; }
    }

    public class MarketingSettings
    {
        [JsonProperty("agent_bio")]
        public string Bio { get; set; }

        [JsonProperty("application_url")]
        public string ApplicationURL { get; set; }

        [JsonProperty("daily_spend_threshold")]
        public string DailySpendThreshold { get; set; }

        [JsonProperty("disclaimer")]
        public string Disclaimer { get; set; }

        [JsonProperty("license_title")]
        public string LicenseTitle { get; set; }

        [JsonProperty("post_close_survey_url")]
        public string PostCloseSurveyUrl { get; set; }

        [JsonProperty("short_name")]
        public string ShortName { get; set; }

        [JsonProperty("social_facebook")]
        public string Facebook { get; set; }

        [JsonProperty("social_google")]
        public string Google { get; set; }

        [JsonProperty("social_linkedin")]
        public string Linkedin { get; set; }

        [JsonProperty("social_twitter")]
        public string Twitter { get; set; }

        [JsonProperty("social_youtube")]
        public string Youtube { get; set; }

        [JsonProperty("user_quote")]
        public string Quote { get; set; }

        [JsonProperty("weekly_spend_threshold")]
        public string WeeklySpendThreshold { get; set; }

        [JsonProperty("wistia_id")]
        public string WistiaID { get; set; }
    }

    public class Team
    {
        [JsonProperty("team_name")]
        public string Name { get; set; }

        [JsonProperty("managers")]
        public UserManager[] Managers { get; set; }
    }

    public class UserManager
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("external_id")]
        public string ExternalIDF { get; set; }

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }
    }
}