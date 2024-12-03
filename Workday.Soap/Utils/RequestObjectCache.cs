using System;
using WorkdayServices.HumanResource;

namespace SharedResources
{
    public static class RequestObjectCache
    {
        public static ObjectCache<Workday_Common_HeaderType> Workday_Common_HeaderType = new ObjectCache<Workday_Common_HeaderType>(() =>
            new Workday_Common_HeaderType()
        );

        public static ObjectCache<Worker_Response_GroupType> Worker_Response_GroupType = new ObjectCache<Worker_Response_GroupType>(() =>
            new Worker_Response_GroupType()
            {
                Include_Account_Provisioning = true,
                Include_Account_ProvisioningSpecified = true,
                Include_Additional_Jobs = true,
                Include_Additional_JobsSpecified = true,
                Include_Background_Check_Data = true,
                Include_Background_Check_DataSpecified = true,
                Include_Benefit_Eligibility = true,
                Include_Benefit_EligibilitySpecified = true,
                Include_Benefit_Enrollments = true,
                Include_Benefit_EnrollmentsSpecified = true,
                Include_Career = true,
                Include_CareerSpecified = true,
                Include_Collective_Agreement_Data = true,
                Include_Collective_Agreement_DataSpecified = true,
                Include_Compensation = true,
                Include_CompensationSpecified = true,
                Include_Contingent_Worker_Tax_Authority_Form_Information = true,
                Include_Contingent_Worker_Tax_Authority_Form_InformationSpecified = true,
                Include_Development_Items = true,
                Include_Development_ItemsSpecified = true,
                Include_Employee_Contract_Data = true,
                Include_Employee_Contract_DataSpecified = true,
                Include_Employee_Review = true,
                Include_Employee_ReviewSpecified = true,
                Include_Employment_Information = true,
                Include_Employment_InformationSpecified = true,
                Include_Extended_Employee_Contract_Details = true,
                Include_Extended_Employee_Contract_DetailsSpecified = true,
                Include_Feedback_Received = true,
                Include_Feedback_ReceivedSpecified = true,
                Include_Goals = true,
                Include_GoalsSpecified = true,
                Include_Management_Chain_Data = true,
                Include_Management_Chain_DataSpecified = true,
                Include_Multiple_Managers_in_Management_Chain_Data = true,
                Include_Multiple_Managers_in_Management_Chain_DataSpecified = true,
                Include_Organizations = true,
                Include_OrganizationsSpecified = true,
                Include_Personal_Information = true,
                Include_Personal_InformationSpecified = true,
                Include_Photo = false,
                Include_PhotoSpecified = true,
                Include_Probation_Period_Data = true,
                Include_Probation_Period_DataSpecified = true,
                Include_Qualifications = true,
                Include_QualificationsSpecified = true,
                Include_Reference = true,
                Include_ReferenceSpecified = true,
                Include_Related_Persons = true,
                Include_Related_PersonsSpecified = true,
                Include_Roles = true,
                Include_RolesSpecified = true,
                Include_Skills = true,
                Include_SkillsSpecified = true,
                Include_Subevents_for_Corrected_Transaction = true,
                Include_Subevents_for_Corrected_TransactionSpecified = true,
                Include_Subevents_for_Rescinded_Transaction = true,
                Include_Subevents_for_Rescinded_TransactionSpecified = true,
                Include_Succession_Profile = true,
                Include_Succession_ProfileSpecified = true,
                Include_Talent_Assessment = true,
                Include_Talent_AssessmentSpecified = true,
                Include_Transaction_Log_Data = true,
                Include_Transaction_Log_DataSpecified = true,
                Include_User_Account = true,
                Include_User_AccountSpecified = true,
                Include_Worker_DocumentsSpecified = true,
                Include_Worker_Documents = false,
                Include_Contracts_for_Terminated_Workers = true,
                Include_Contracts_for_Terminated_WorkersSpecified = true,
                Exclude_Teams = false,
                Exclude_TeamsSpecified = true,
                Exclude_Business_Units = false,
                Exclude_Business_UnitsSpecified = true,
                Exclude_Business_Unit_Hierarchies = false,
                Exclude_Business_Unit_HierarchiesSpecified = true,
                Exclude_Companies = false,
                Exclude_CompaniesSpecified = true,
                Exclude_Company_Hierarchies = false,
                Exclude_Company_HierarchiesSpecified = true,
                Exclude_Cost_Centers = false,
                Exclude_Cost_CentersSpecified = true,
                Exclude_Cost_Center_Hierarchies = false,
                Exclude_Cost_Center_HierarchiesSpecified = true,
                Exclude_Custom_Organizations = false,
                Exclude_Custom_OrganizationsSpecified = true,
                Exclude_Funds = false,
                Exclude_FundsSpecified = true,
                Exclude_Fund_Hierarchies = false,
                Exclude_Fund_HierarchiesSpecified = true,
                Exclude_Gifts = false,
                Exclude_GiftsSpecified = true,
                Exclude_Gift_Hierarchies = false,
                Exclude_Gift_HierarchiesSpecified = true,
                Exclude_Grants = false,
                Exclude_GrantsSpecified = true,
                Exclude_Grant_Hierarchies = false,
                Exclude_Grant_HierarchiesSpecified = true,
                Exclude_Location_Hierarchies = false,
                Exclude_Location_HierarchiesSpecified = true,
                Exclude_Matrix_Organizations = false,
                Exclude_Matrix_OrganizationsSpecified = true,
                Exclude_Organization_Support_Role_Data = false,
                Exclude_Organization_Support_Role_DataSpecified = true,
                Exclude_Pay_Groups = false,
                Exclude_Pay_GroupsSpecified = true,
                Exclude_Programs = false,
                Exclude_ProgramsSpecified = true,
                Exclude_Program_Hierarchies = false,
                Exclude_Program_HierarchiesSpecified = true,
                Exclude_Regions = false,
                Exclude_RegionsSpecified = true,
                Exclude_Region_Hierarchies = false,
                Exclude_Region_HierarchiesSpecified = true,
                Exclude_Supervisory_Organizations = false,
                Exclude_Supervisory_OrganizationsSpecified = true
            }
        );

        public static ObjectCache<Worker_Response_GroupType> Worker_Response_GroupTypeLite = new ObjectCache<Worker_Response_GroupType>(() =>
           new Worker_Response_GroupType()
           {
               Include_Personal_Information = true,
               Include_Personal_InformationSpecified = true,
               Include_Employment_Information = true,
               Include_Employment_InformationSpecified = true,
               Include_Organizations = true,
               Include_OrganizationsSpecified = true,
               Include_Management_Chain_Data = true,
               Include_Management_Chain_DataSpecified = true,
               Include_Multiple_Managers_in_Management_Chain_Data = true,
               Include_Multiple_Managers_in_Management_Chain_DataSpecified = true,
               Include_Account_Provisioning = false,
               Include_Account_ProvisioningSpecified = true,
               Include_Additional_Jobs = false,
               Include_Additional_JobsSpecified = true,
               Include_Background_Check_Data = false,
               Include_Background_Check_DataSpecified = true,
               Include_Benefit_Eligibility = false,
               Include_Benefit_EligibilitySpecified = true,
               Include_Benefit_Enrollments = false,
               Include_Benefit_EnrollmentsSpecified = true,
               Include_Career = false,
               Include_CareerSpecified = true,
               Include_Collective_Agreement_Data = false,
               Include_Collective_Agreement_DataSpecified = true,
               Include_Compensation = false,
               Include_CompensationSpecified = true,
               Include_Contingent_Worker_Tax_Authority_Form_Information = false,
               Include_Contingent_Worker_Tax_Authority_Form_InformationSpecified = true,
               Include_Development_Items = false,
               Include_Development_ItemsSpecified = true,
               Include_Employee_Contract_Data = false,
               Include_Employee_Contract_DataSpecified = true,
               Include_Employee_Review = false,
               Include_Employee_ReviewSpecified = true,
               Include_Extended_Employee_Contract_Details = false,
               Include_Extended_Employee_Contract_DetailsSpecified = true,
               Include_Feedback_Received = false,
               Include_Feedback_ReceivedSpecified = true,
               Include_Goals = false,
               Include_GoalsSpecified = true,
               Include_Photo = false,
               Include_PhotoSpecified = true,
               Include_Probation_Period_Data = false,
               Include_Probation_Period_DataSpecified = true,
               Include_Qualifications = false,
               Include_QualificationsSpecified = true,
               Include_Reference = false,
               Include_ReferenceSpecified = true,
               Include_Related_Persons = false,
               Include_Related_PersonsSpecified = true,
               Include_Roles = false,
               Include_RolesSpecified = true,
               Include_Skills = false,
               Include_SkillsSpecified = true,
               Include_Subevents_for_Corrected_Transaction = false,
               Include_Subevents_for_Corrected_TransactionSpecified = true,
               Include_Subevents_for_Rescinded_Transaction = false,
               Include_Subevents_for_Rescinded_TransactionSpecified = true,
               Include_Succession_Profile = false,
               Include_Succession_ProfileSpecified = true,
               Include_Talent_Assessment = false,
               Include_Talent_AssessmentSpecified = true,
               Include_Transaction_Log_Data = false,
               Include_Transaction_Log_DataSpecified = true,
               Include_User_Account = false,
               Include_User_AccountSpecified = true,
               Include_Worker_Documents = false,
               Include_Worker_DocumentsSpecified = false,
               Include_Contracts_for_Terminated_Workers = false,
               Include_Contracts_for_Terminated_WorkersSpecified = true,
               Exclude_Business_Units = false,
               Exclude_Business_UnitsSpecified = true,
               Exclude_Cost_Centers = false,
               Exclude_Cost_CentersSpecified = true,
               Exclude_Regions = false,
               Exclude_RegionsSpecified = true,
               Exclude_Organization_Support_Role_Data = false,
               Exclude_Organization_Support_Role_DataSpecified = true,
               Exclude_Teams = true,
               Exclude_TeamsSpecified = true,
               Exclude_Business_Unit_Hierarchies = true,
               Exclude_Business_Unit_HierarchiesSpecified = true,
               Exclude_Companies = true,
               Exclude_CompaniesSpecified = true,
               Exclude_Company_Hierarchies = true,
               Exclude_Company_HierarchiesSpecified = true,
               Exclude_Cost_Center_Hierarchies = true,
               Exclude_Cost_Center_HierarchiesSpecified = true,
               Exclude_Custom_Organizations = true,
               Exclude_Custom_OrganizationsSpecified = true,
               Exclude_Funds = true,
               Exclude_FundsSpecified = true,
               Exclude_Fund_Hierarchies = true,
               Exclude_Fund_HierarchiesSpecified = true,
               Exclude_Gifts = true,
               Exclude_GiftsSpecified = true,
               Exclude_Gift_Hierarchies = true,
               Exclude_Gift_HierarchiesSpecified = true,
               Exclude_Grants = true,
               Exclude_GrantsSpecified = true,
               Exclude_Grant_Hierarchies = true,
               Exclude_Grant_HierarchiesSpecified = true,
               Exclude_Location_Hierarchies = true,
               Exclude_Location_HierarchiesSpecified = true,
               Exclude_Matrix_Organizations = true,
               Exclude_Matrix_OrganizationsSpecified = true,
               Exclude_Pay_Groups = true,
               Exclude_Pay_GroupsSpecified = true,
               Exclude_Programs = true,
               Exclude_ProgramsSpecified = true,
               Exclude_Program_Hierarchies = true,
               Exclude_Program_HierarchiesSpecified = true,
               Exclude_Region_Hierarchies = true,
               Exclude_Region_HierarchiesSpecified = true,
               Exclude_Supervisory_Organizations = true,
               Exclude_Supervisory_OrganizationsSpecified = true,
           }
       );

        public static ObjectCache<Communication_Method_Usage_Information_DataType[]> Email_Update_Usage_Data_WorkValue = new ObjectCache<Communication_Method_Usage_Information_DataType[]>(() =>
            new Communication_Method_Usage_Information_DataType[]
            {
                new Communication_Method_Usage_Information_DataType
                {
                    Type_Data = new Communication_Usage_Type_DataType[]
                    {
                        new Communication_Usage_Type_DataType
                        {
                            Type_Reference = new Communication_Usage_TypeObjectType
                            {
                                    ID = new Communication_Usage_TypeObjectIDType[]
                                    {
                                        new Communication_Usage_TypeObjectIDType
                                        {
                                            type = "Communication_Usage_Type_ID",
                                            Value = "WORK"
                                        }
                                    }
                            },
                            Primary = true,
                            PrimarySpecified = true
                        }
                    },
                    Public = true,
                    PublicSpecified = true
                }
            }
        );

        public static ObjectCache<Business_Process_ParametersType> Business_Process_ParametersType = new ObjectCache<Business_Process_ParametersType>(() =>
           new Business_Process_ParametersType
           {
               Auto_Complete = true,
               Auto_CompleteSpecified = true,
               Run_Now = true,
               Run_NowSpecified = true
           }
       );
    }


    public class ObjectCache<T>
    {
        private readonly Func<T> _setObject;
        private T _objectValue { get; set; }

        public T Value
        {
            get
            {
                if (_objectValue == null)
                    _objectValue = _setObject.Invoke();
                return _objectValue;
            }
        }

        public ObjectCache(Func<T> setObject) => _setObject = setObject;
    }
}
