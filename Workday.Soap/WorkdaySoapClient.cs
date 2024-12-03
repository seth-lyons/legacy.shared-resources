using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WorkdayServices.HumanResource;

namespace SharedResources
{
    public class WorkdaySoapClient : IDisposable
    {
        private Human_ResourcesPortClient _hrClient;
        private WorkdaySoapSettings _settings;

        static WorkdaySoapClient()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public WorkdaySoapClient(string username, string password, WorkdayEnvironment workdayEnvironment = WorkdayEnvironment.Production)
            : this(new WorkdaySoapSettings { Username = username, Password = password, Environment = workdayEnvironment })
        {
        }

        public WorkdaySoapClient(WorkdaySoapSettings settings)
        {
            _settings = settings;
            if (_settings.BaseAddress.IsEmpty())
            {
                _settings.BaseAddress =
                    _settings.Environment == WorkdayEnvironment.Preview ? "https://wd2-impl-services1.workday.com/ccx/service/nlcloans_preview/"
                  : _settings.Environment == WorkdayEnvironment.Sandbox ? "https://wd2-impl-services1.workday.com/ccx/service/nlcloans/"
                  : "https://services1.myworkday.com/ccx/service/nlcloans/";
            }
            if (_settings.ApiVersion.IsEmpty())
                _settings.ApiVersion = "v36.1";

            InitializeHumanResourcesClient(_settings.Environment == WorkdayEnvironment.Preview ? "nlcloans_preview" : "nlcloans");
        }

        public void InitializeHumanResourcesClient(string tenant = "nlcloans")
        {
            SecurityBindingElement sb = SecurityBindingElement.CreateUserNameOverTransportBindingElement();
            sb.IncludeTimestamp = false;
            var timeout = TimeSpan.FromMinutes(5);

            _hrClient = new Human_ResourcesPortClient(
                new CustomBinding(
                sb,
                new TextMessageEncodingBindingElement(MessageVersion.Soap11, Encoding.UTF8)
                {
                    ReaderQuotas = new XmlDictionaryReaderQuotas
                    {
                        MaxDepth = Int32.MaxValue,
                        MaxStringContentLength = Int32.MaxValue,
                        MaxArrayLength = Int32.MaxValue,
                        MaxBytesPerRead = Int32.MaxValue,
                        MaxNameTableCharCount = Int32.MaxValue
                    }
                },
                new HttpsTransportBindingElement
                {
                    MaxReceivedMessageSize = Int32.MaxValue,
                    MaxBufferSize = Int32.MaxValue
                })
                {
                    SendTimeout = timeout,
                    ReceiveTimeout = timeout
                },
                new EndpointAddress($"{_settings.BaseAddress}Human_Resources/{_settings.ApiVersion}")
            );
            _hrClient.ClientCredentials.UserName.UserName = _settings.Username + $"@{tenant}";
            _hrClient.ClientCredentials.UserName.Password = _settings.Password;
        }

        public void Dispose()
        {
            if (_hrClient != null)
            {
                _hrClient.Abort();
                _hrClient.Close();
            }
        }

        public async Task<Change_Work_Contact_InformationOutput> UpdateWorkerEmail(string empID, string newEmail, bool contingentWorker = false)
        {
            return await _hrClient.Change_Work_Contact_InformationAsync(
                RequestObjectCache.Workday_Common_HeaderType.Value,
                new Change_Work_Contact_Information_RequestType
                {
                    version = _settings.ApiVersion,
                    Business_Process_Parameters = RequestObjectCache.Business_Process_ParametersType.Value,
                    Change_Work_Contact_Information_Data = new Change_Work_Contact_Information_Business_Process_DataType
                    {
                        Person_Reference = new RoleObjectType
                        {
                            ID = new RoleObjectIDType[]
                              {
                                  new RoleObjectIDType
                                  {
                                       type = contingentWorker ? "Contingent_Worker_ID" : "Employee_ID",
                                       Value = empID
                                  }
                              }
                        },
                        Event_Effective_DateSpecified = true,
                        Event_Effective_Date = DateTime.Now,
                        Person_Contact_Information_Data = new Person_Contact_Information_DataType
                        {
                            Person_Email_Information_Data = new Person_Email_Information_DataType
                            {
                                Email_Information_Data = new Person_Email_DataType[]
                                {
                                    new Person_Email_DataType
                                    {
                                        Email_Data = new Email_Core_DataType[]
                                        {
                                            new Email_Core_DataType
                                            {
                                                 Email_Address = newEmail
                                            }
                                        },
                                        Usage_Data = RequestObjectCache.Email_Update_Usage_Data_WorkValue.Value
                                    }
                                }
                            }
                        }
                    }
                });
        }

        public async Task<Put_Worker_PhotoOutput> UpdateWorkerPhoto(string empID, string fileName, byte[] photo)
        {
            return await _hrClient.Put_Worker_PhotoAsync(
                RequestObjectCache.Workday_Common_HeaderType.Value,
                new Put_Worker_Photo_RequestType
                {
                    version = _settings.ApiVersion,
                    Worker_Reference = new WorkerObjectType()
                    {
                        ID = new WorkerObjectIDType[] {
                            new WorkerObjectIDType()
                            {
                                type = "Employee_ID",
                                Value = empID
                            }
                        }
                    },
                    Worker_Photo_Data = new Employee_Photo_DataType
                    {
                        File = photo,
                        Filename = fileName
                    }
                });
        }

        public async Task<Change_Work_Contact_InformationOutput> UpdateWorkerPhone(string empID, string newPhone)
        {
            return await _hrClient.Change_Work_Contact_InformationAsync(
                RequestObjectCache.Workday_Common_HeaderType.Value,
                new Change_Work_Contact_Information_RequestType
                {
                    version = _settings.ApiVersion,
                    Business_Process_Parameters = RequestObjectCache.Business_Process_ParametersType.Value,
                    Change_Work_Contact_Information_Data = new Change_Work_Contact_Information_Business_Process_DataType
                    {
                        Person_Reference = new RoleObjectType
                        {
                            ID = new RoleObjectIDType[]
                              {
                                  new RoleObjectIDType
                                  {
                                       type = "Employee_ID",
                                       Value = empID
                                  }
                              }
                        },
                        Event_Effective_DateSpecified = true,
                        Event_Effective_Date = DateTime.Now,
                        Person_Contact_Information_Data = new Person_Contact_Information_DataType
                        {
                            Person_Phone_Information_Data = new Person_Phone_Information_DataType
                            {
                                Phone_Information_Data = new Person_Phone_DataType[] {
                                    new Person_Phone_DataType
                                    {
                                        Phone_Data = new Phone_Core_DataType[]
                                        {
                                            new Phone_Core_DataType
                                            {
                                                Complete_Phone_Number = newPhone,
                                                Country_Code_Reference = new Country_Phone_CodeObjectType
                                                {
                                                    ID = new Country_Phone_CodeObjectIDType[]{
                                                        new Country_Phone_CodeObjectIDType{
                                                            type = "Country_Phone_Code_ID",
                                                            Value = "USA_1"
                                                        }
                                                    }
                                                },
                                                Device_Type_Reference = new Phone_Device_TypeObjectType
                                                {
                                                    ID = new Phone_Device_TypeObjectIDType[] {
                                                        new Phone_Device_TypeObjectIDType
                                                        {
                                                            type = "Phone_Device_Type_ID",
                                                            Value = "Landline"
                                                        }
                                                    }
                                                }
                                            }
                                        },
                                        Usage_Data = RequestObjectCache.Email_Update_Usage_Data_WorkValue.Value
                                    }
                               }
                            }
                        }
                    }
                });
        }
    }
}
