using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SharedResources
{
    public static class CreditProcessor
    {
        public static CreditPackage ProcessXML(XDocument report)
        {
            var ns = report.Root.Name.Namespace;

            var response = report?.Descendants(ns + "CREDIT_RESPONSE")?.FirstOrDefault();
            var relationships = GetRelationships(report, ns);

            var bureau = GetCreditBureau(response, ns);
            var creditReferrals = GetCreditReferrals(response, ns);
            var creditFiles = GetCreditFiles(response, creditReferrals, ns);
            var creditInquiries = GetCreditInquiries(response, ns);
            var CreditLiabilities = GetCreditLiabilities(response, ns);
            var creditScoreModels = GetCreditScoreModels(response, ns);
            var creditScores = GetCreditScores(response, ns);
            var roles = GetRoles(response, ns);

            foreach (var relationship in relationships?.Where(r => r.RelationshipType.Is("IsAssociatedWith") && (r.Item1Type.Is("CREDIT_FILE") || r.Item2Type.Is("CREDIT_FILE"))))
            {
                string relatedItemType = relationship.Item1Type.Is("CREDIT_FILE") ? relationship.Item2Type : relationship.Item1Type;
                string creditFileID = null;
                string relatedObjectID = null;

                if (relationship.Item1.StartsWith("CreditFile_"))
                {
                    creditFileID = relationship.Item1;
                    relatedObjectID = relationship.Item2;
                }
                else
                {
                    relatedObjectID = relationship.Item1;
                    creditFileID = relationship.Item2;
                }

                var creditFile = creditFiles.FirstOrDefault(a => a.Label == creditFileID);
                if (creditFile != null)
                {
                    if (relatedItemType.Is("ROLE"))
                        creditFile.Role = roles.FirstOrDefault(a => a.Label == relatedObjectID);
                    else if (relatedItemType.Is("CREDIT_SCORE"))
                        creditFile.CreditScore = creditScores.FirstOrDefault(a => a.Label == relatedObjectID);
                    else if (relatedItemType.Is("CREDIT_SCORE_MODEL"))
                        creditFile.CreditScoreModel = creditScoreModels.FirstOrDefault(a => a.Label == relatedObjectID);
                    else if (relatedItemType.Is("CREDIT_INQUIRY"))
                    {
                        if (creditFile.CreditInquries == null) creditFile.CreditInquries = new List<CreditInquiry>();
                        creditFile.CreditInquries.Add(creditInquiries.FirstOrDefault(a => a.Label == relatedObjectID));
                    }
                    else if (relatedItemType.Is("CREDIT_LIABILITY"))
                    {
                        if (creditFile.CreditLiabilities == null) creditFile.CreditLiabilities = new List<CreditLiability>();
                        creditFile.CreditLiabilities.Add(CreditLiabilities.FirstOrDefault(a => a.Label == relatedObjectID));
                    }
                }
            }

            var groups = creditFiles
                ?.GroupBy(x => x.Role.Label)
                ?.Select(x => new CreditFileGroup
                {
                    Identifiers = x
                        ?.SelectMany(cf => new[] { cf.Identifier, cf.Role.Identifier })
                        ?.Where(id => !string.IsNullOrWhiteSpace(id))
                        ?.Distinct()
                        ?.ToArray(),
                    CreditFiles = x.ToList()
                })
                ?.ToArray();

            //var groups = creditFiles
            //    ?.GroupBy(x => Regex.Replace(x.Identifier ?? "", @"[^0-9]", ""))
            //    ?.ToDictionary(x => x.Key, x => x.ToList());

            var borrowerCount = groups?.Count() ?? 0;

            return new CreditPackage
            {
                DenialReasons = new DenialReasons
                {
                    DelinquentCreditObligations = true,
                },
                Bureau = bureau,
                CreditFileGroups = groups,
                IsError = false
            };
        }

        public static List<Relationship> GetRelationships(XDocument report, XNamespace ns = null)
        {
            ns = ns ?? report.Root.GetDefaultNamespace();
            return report
                 ?.Descendants(ns + "RELATIONSHIP")
                 ?.Select(r =>
                 {
                     var d7p1 = r.GetNamespaceOfPrefix("d7p1");
                     var details = r?.Attribute(d7p1 + "arcrole")?.Value;
                     var match = Regex.Match(details, @".*\/(.*)?_(IsAssociatedWith|IsVerifiedBy)_(.*)");
                     return new Relationship
                     {
                         SequenceNumber = r?.Attribute("SequenceNumber")?.Value,
                         Item1 = r?.Attribute(d7p1 + "from")?.Value,
                         Item2 = r?.Attribute(d7p1 + "to")?.Value,
                         Item1Type = match.Groups.Count > 1 ? match.Groups[1].Value : null,
                         RelationshipType = match.Groups.Count > 2 ? match.Groups[2].Value : null,
                         Item2Type = match.Groups.Count > 3 ? match.Groups[3].Value : null,
                     };
                 })?.ToList();
        }

        public static Business GetCreditBureau(XElement response, XNamespace ns = null)
        {
            ns = ns ?? response.GetDefaultNamespace();
            var bureau = response?.Element(ns + "CREDIT_BUREAU");
            var bureauAddress = bureau?.Element(ns + "ADDRESS");
            var bureauName = bureau?.NestedElement("CREDIT_BUREAU_DETAIL/CreditBureauName")?.Value;
            var bureauphone = Operations.FormatPhone(bureau?.NestedElement("CONTACT_POINTS/CONTACT_POINT/CONTACT_POINT_TELEPHONE/ContactPointTelephoneValue")?.Value);

            return new Business
            {
                Name = bureauName,
                Phone = bureauphone,
                Location = new Location
                {
                    Address = bureauAddress?.Element(ns + "AddressLineText")?.Value,
                    City = bureauAddress?.Element(ns + "CityName")?.Value,
                    State = bureauAddress?.Element(ns + "StateCode")?.Value,
                    Zip = bureauAddress?.Element(ns + "PostalCode")?.Value
                }
            };
        }

        public static List<CreditScoreModel> GetCreditScoreModels(XElement response, XNamespace ns = null)
        {
            ns = ns ?? response.GetDefaultNamespace();
            return response
                ?.Element(ns + "CREDIT_SCORE_MODELS")
                ?.Elements()
                ?.Select(csm =>
                {
                    var d7p1 = csm.GetNamespaceOfPrefix("d7p1");
                    return new CreditScoreModel
                    {
                        SequenceNumber = csm?.Attribute("SequenceNumber")?.Value,
                        Label = csm?.Attribute(d7p1 + "label")?.Value,
                        Name = csm?.NestedElement("CREDIT_SCORE_MODEL_DETAIL/CreditScoreModelNameType")?.Value,
                        Max = csm?.NestedElement("CREDIT_SCORE_MODEL_DETAIL/CreditScoreMaximumValue")?.Value?.TrimStart('0'),
                        Min = csm?.NestedElement("CREDIT_SCORE_MODEL_DETAIL/CreditScoreMinimumValue")?.Value?.TrimStart('0'),
                    };
                })?.ToList();
        }

        public static List<CreditScore> GetCreditScores(XElement response, XNamespace ns = null)
        {
            ns = ns ?? response.GetDefaultNamespace();
            return response
                ?.Element(ns + "CREDIT_SCORES")
                ?.Elements()
                ?.Select(cs =>
                {
                    var d7p1 = cs.GetNamespaceOfPrefix("d7p1");
                    return new CreditScore
                    {
                        SequenceNumber = cs?.Attribute("SequenceNumber")?.Value,
                        Label = cs?.Attribute(d7p1 + "label")?.Value,
                        Date = DateTime.TryParse(cs?.NestedElement("CREDIT_SCORE_DETAIL/CreditScoreDate")?.Value, out DateTime cDate) ? (DateTime?)cDate : null,
                        // Was told by Tony V and Lindsey B to always check the Facta box - 3/12/2021
                        FACTAInquiriesIndicator = true,
                        //FACTAInquiriesIndicator = bool.TryParse(cs?.NestedElement("CREDIT_SCORE_DETAIL/CreditScoreFACTAInquiriesIndicator")?.Value, out bool factaInq) ? factaInq : false,
                        // Model = models.FirstOrDefault(m => m.Name.Is(cs?.NestedElement("CREDIT_SCORE_DETAIL/CreditScoreModelNameType")?.Value)),
                        CreditScoreRankPercentile = cs?.NestedElement("CREDIT_SCORE_DETAIL/CreditScoreRankPercentileValue")?.Value,
                        CreditScoreValue = cs?.NestedElement("CREDIT_SCORE_DETAIL/CreditScoreValue")?.Value?.TrimStart('0'),
                        Factors = cs?.Element(ns + "CREDIT_SCORE_FACTORS")
                            ?.Elements()
                            ?.Select(f =>
                            {
                                return new CreditScoreFactor
                                {
                                    Code = f.Element(ns + "CreditScoreFactorCode")?.Value,
                                    Text = f.Element(ns + "CreditScoreFactorText")?.Value,
                                };
                            })?.ToList()
                    };
                })?.ToList();
        }

        public static List<Business> GetCreditReferrals(XElement response, XNamespace ns = null)
        {
            ns = ns ?? response.GetDefaultNamespace();
            return response
                ?.Element(ns + "CREDIT_CONSUMER_REFERRALS")
                ?.Elements()
                ?.Select(cr =>
                {
                    var address = cr?.Element(ns + "ADDRESS");
                    var city = address?.Element(ns + "CityName")?.Value;
                    return new Business
                    {
                        SequenceNumber = cr?.Attribute("SequenceNumber")?.Value,
                        Name = city.Is("CHESTER") ? "TransUnion" : cr?.NestedElement("NAME/FullName")?.Value, //TODO: Why do I have to do this?
                        Phone = Operations.FormatPhone(cr?.NestedElement("CONTACT_POINTS/CONTACT_POINT/CONTACT_POINT_TELEPHONE/ContactPointTelephoneValue")?.Value),
                        Location = new Location
                        {
                            Address = address?.Element(ns + "AddressLineText")?.Value,
                            City = city,
                            State = address?.Element(ns + "StateCode")?.Value,
                            Zip = address?.Element(ns + "PostalCode")?.Value
                        }
                    };
                })?.ToList();
        }

        public static List<CreditFile> GetCreditFiles(XElement response, List<Business> referrers, XNamespace ns = null)
        {
            ns = ns ?? response.GetDefaultNamespace();
            return response
                ?.Element(ns + "CREDIT_FILES")
                ?.Elements()
                ?.Select(x =>
                {
                    var d7p1 = x.GetNamespaceOfPrefix("d7p1");
                    var details = x?.Element(ns + "CREDIT_FILE_DETAIL");
                    var source = details?.Element(ns + "CreditRepositorySourceType")?.Value;
                    return new CreditFile
                    {
                        SequenceNumber = x?.Attribute("SequenceNumber")?.Value,
                        Label = x?.Attribute(d7p1 + "label")?.Value,
                        Status = details?.Element(ns + "CreditFileResultStatusType")?.Value,
                        Source = source,
                        Identifier = x?.NestedElement("PARTY/TAXPAYER_IDENTIFIERS/TAXPAYER_IDENTIFIER/TaxpayerIdentifierValue")?.Value,
                        InfileDate = DateTime.TryParse(details?.Element(ns + "CreditFileInfileDate")?.Value, out DateTime cDate) ? (DateTime?)cDate : null,
                        Referrer = referrers?.FirstOrDefault(a => a.Name.Is(source))
                    };
                })?.ToList();
        }

        public static List<CreditInquiry> GetCreditInquiries(XElement response, XNamespace ns = null)
        {
            ns = ns ?? response.GetDefaultNamespace();
            return response
                ?.Element(ns + "CREDIT_INQUIRIES")
                ?.Elements()
                ?.Select(x =>
                {
                    var d7p1 = x.GetNamespaceOfPrefix("d7p1");
                    var details = x?.Element(ns + "CREDIT_INQUIRY_DETAIL");
                    return new CreditInquiry
                    {
                        SequenceNumber = x?.Attribute("SequenceNumber")?.Value,
                        Label = x?.Attribute(d7p1 + "label")?.Value,
                        CreditBusinessType = details?.Element(ns + "CreditBusinessType")?.Value,
                        InquiryDate = DateTime.TryParse(details?.Element(ns + "CreditInquiryDate")?.Value, out DateTime cDate) ? (DateTime?)cDate : null,
                        DetailCreditBusinessType = details?.Element(ns + "DetailCreditBusinessType")?.Value,
                        Name = x?.NestedElement("NAME/FullName")?.Value,
                        CreditRepositories = x
                            ?.Element(ns + "CREDIT_REPOSITORIES")
                            ?.Elements()
                            ?.Select(r => new CreditRepository
                            {
                                Code = r?.Element(ns + "CreditRepositorySubscriberCode")?.Value,
                                Description = r?.Element(ns + "CreditRepositorySourceTypeOtherDescription")?.Value,
                                Type = r?.Element(ns + "CreditRepositorySourceType")?.Value
                            })
                            ?.ToArray()
                    };
                })?.ToList();
        }

        public static List<CreditLiability> GetCreditLiabilities(XElement response, XNamespace ns = null)
        {
            ns = ns ?? response.GetDefaultNamespace();
            return response
                ?.Element(ns + "CREDIT_LIABILITIES")
                ?.Elements()
                ?.Select(x =>
                {
                    var d7p1 = x.GetNamespaceOfPrefix("d7p1");
                    var details = x?.Element(ns + "CREDIT_LIABILITY_DETAIL");
                    var creditor = x?.Element(ns + "CREDIT_LIABILITY_CREDITOR");
                    var creditorAddress = creditor?.Element(ns + "ADDRESS");
                    return new CreditLiability
                    {
                        SequenceNumber = x?.Attribute("SequenceNumber")?.Value,
                        Label = x?.Attribute(d7p1 + "label")?.Value,
                        Creditor = new Creditor
                        {
                            Name = creditor?.NestedElement("NAME/FullName")?.Value,
                            ClassificationCode = creditor?.NestedElement("EXTENSION/OTHER/CreditorClassificationCode")?.Value,
                            Phone = creditor?.NestedElement("CONTACT_POINTS/CONTACT_POINT/CONTACT_POINT_TELEPHONE/ContactPointTelephoneValue")?.Value,
                            Location = new Location
                            {
                                Address = creditorAddress?.Element(ns + "AddressLineText")?.Value,
                                City = creditorAddress?.Element(ns + "CityName")?.Value,
                                State = creditorAddress?.Element(ns + "StateCode")?.Value,
                                Zip = creditorAddress?.Element(ns + "PostalCode")?.Value,
                            }
                        },
                        LiabilityRating = new CreditRating
                        {
                            Code = x?.NestedElement("CREDIT_LIABILITY_CURRENT_RATING/CreditLiabilityCurrentRatingCode")?.Value,
                            Type = x?.NestedElement("CREDIT_LIABILITY_CURRENT_RATING/CreditLiabilityCurrentRatingType")?.Value,
                        },
                        CreditBusinessType = details?.Element(ns + "CreditBusinessType")?.Value,
                        ClosedDate = DateTime.TryParse(details?.Element(ns + "CreditLiabilityAccountClosedDate")?.Value, out DateTime d1) ? (DateTime?)d1 : null,
                        AccountOpenedDate = DateTime.TryParse(details?.Element(ns + "CreditLiabilityAccountOpenedDate")?.Value, out DateTime d2) ? (DateTime?)d2 : null,
                        LastActivityDate = DateTime.TryParse(details?.Element(ns + "CreditLiabilityLastActivityDate")?.Value, out DateTime d3) ? (DateTime?)d3 : null,
                        ReportedDate = DateTime.TryParse(details?.Element(ns + "CreditLiabilityAccountReportedDate")?.Value, out DateTime d4) ? (DateTime?)d4 : null,
                        AccountPaidDate = DateTime.TryParse(details?.Element(ns + "CreditLiabilityAccountPaidDate")?.Value, out DateTime d6) ? (DateTime?)d6 : null,
                        PaymentPatternStartDate = DateTime.TryParse(x?.NestedElement("CREDIT_LIABILITY_PAYMENT_PATTERN/CreditLiabilityPaymentPatternStartDate")?.Value, out DateTime d5) ? (DateTime?)d5 : null,
                        DetailCreditBusinessType = details?.Element(ns + "DetailCreditBusinessType")?.Value,
                        StatusType = details?.Element(ns + "CreditLiabilityAccountStatusType")?.Value,
                        TermsSourceType = details?.Element(ns + "CreditLiabilityTermsSourceType")?.Value,
                        AccountIdentifier = details?.Element(ns + "CreditLiabilityAccountIdentifier")?.Value,
                        AccountType = details?.Element(ns + "CreditLiabilityAccountType")?.Value,
                        LoanType = details?.Element(ns + "CreditLoanType")?.Value,
                        OwnershipType = details?.Element(ns + "CreditLiabilityAccountOwnershipType")?.Value,
                        TermsDescription = details?.Element(ns + "CreditLiabilityTermsDescription")?.Value,
                        ConsumerDisputeIndicator = bool.TryParse(details?.Element(ns + "CreditLiabilityConsumerDisputeIndicator")?.Value, out bool b1) ? (bool?)b1 : null,
                        CreditLimitAmount = decimal.TryParse(details?.Element(ns + "CreditLiabilityCreditLimitAmount")?.Value, out decimal dec1) ? (decimal?)dec1 : null,
                        ChargeOffAmount = decimal.TryParse(details?.Element(ns + "CreditLiabilityChargeOffAmount")?.Value, out decimal dec2) ? (decimal?)dec2 : null,
                        HighBalanceAmount = decimal.TryParse(details?.Element(ns + "CreditLiabilityHighBalanceAmount")?.Value, out decimal dec3) ? (decimal?)dec3 : null,
                        MonthlyPaymentAmount = decimal.TryParse(details?.Element(ns + "CreditLiabilityMonthlyPaymentAmount")?.Value, out decimal dec4) ? (decimal?)dec4 : null,
                        PastDueAmount = decimal.TryParse(details?.Element(ns + "CreditLiabilityPastDueAmount")?.Value, out decimal dec5) ? (decimal?)dec5 : null,
                        UnpaidBalanceAmount = decimal.TryParse(details?.Element(ns + "CreditLiabilityUnpaidBalanceAmount")?.Value, out decimal dec6) ? (decimal?)dec6 : null,
                        LateCount_30Days = int.TryParse(x?.NestedElement("CREDIT_LIABILITY_LATE_COUNT/CREDIT_LIABILITY_LATE_COUNT_DETAIL/CreditLiability30DaysLateCount")?.Value, out int i1) ? (int?)i1 : null,
                        LateCount_60Days = int.TryParse(x?.NestedElement("CREDIT_LIABILITY_LATE_COUNT/CREDIT_LIABILITY_LATE_COUNT_DETAIL/CreditLiability60DaysLateCount")?.Value, out int i2) ? (int?)i2 : null,
                        LateCount_90Days = int.TryParse(x?.NestedElement("CREDIT_LIABILITY_LATE_COUNT/CREDIT_LIABILITY_LATE_COUNT_DETAIL/CreditLiability90DaysLateCount")?.Value, out int i3) ? (int?)i3 : null,
                        TermsMonthsCount = int.TryParse(details?.Element(ns + "CreditLiabilityTermsMonthsCount")?.Value, out int i4) ? (int?)i4 : null,
                        MonthsReviewedCount = int.TryParse(details?.Element(ns + "CreditLiabilityMonthsReviewedCount")?.Value, out int i5) ? (int?)i5 : null,
                        PaymentPatternDataText = x?.NestedElement("CREDIT_LIABILITY_PAYMENT_PATTERN/CreditLiabilityPaymentPatternDataText")?.Value,
                        HighestAdverseRating = new CreditRating
                        {
                            Code = x?.NestedElement("CREDIT_LIABILITY_HIGHEST_ADVERSE_RATING/CreditLiabilityHighestAdverseRatingCode")?.Value,
                            Type = x?.NestedElement("CREDIT_LIABILITY_HIGHEST_ADVERSE_RATING/CreditLiabilityHighestAdverseRatingType")?.Value,
                            Date = DateTime.TryParse(x?.NestedElement("CREDIT_LIABILITY_HIGHEST_ADVERSE_RATING/CreditLiabilityHighestAdverseRatingDate")?.Value, out DateTime d7) ? (DateTime?)d7 : null
                        },
                        MostRecentAdverseRating = new CreditRating
                        {
                            Code = x?.NestedElement("CREDIT_LIABILITY_MOST_RECENT_ADVERSE_RATING/CreditLiabilityMostRecentAdverseRatingCode")?.Value,
                            Type = x?.NestedElement("CREDIT_LIABILITY_MOST_RECENT_ADVERSE_RATING/CreditLiabilityMostRecentAdverseRatingType")?.Value,
                            Date = DateTime.TryParse(x?.NestedElement("CREDIT_LIABILITY_MOST_RECENT_ADVERSE_RATING/CreditLiabilityMostRecentAdverseRatingDate")?.Value, out DateTime d8) ? (DateTime?)d8 : null
                        },
                        PriorAdverseRatings = x
                            ?.Element(ns + "CREDIT_LIABILITY_PRIOR_ADVERSE_RATINGS")
                            ?.Elements()
                            ?.Select(r => new CreditRating
                            {
                                Code = r?.Element(ns + "CreditLiabilityPriorAdverseRatingCode")?.Value,
                                Type = r?.Element(ns + "CreditLiabilityPriorAdverseRatingType")?.Value,
                                Date = DateTime.TryParse(r?.Element(ns + "CreditLiabilityPriorAdverseRatingDate")?.Value, out DateTime d9) ? (DateTime?)d9 : null
                            })
                            ?.ToArray(),
                        CreditRepositories = x
                            ?.Element(ns + "CREDIT_REPOSITORIES")
                            ?.Elements()
                            ?.Select(r => new CreditRepository
                            {
                                Code = r?.Element(ns + "CreditRepositorySubscriberCode")?.Value,
                                Description = r?.Element(ns + "CreditRepositorySourceTypeOtherDescription")?.Value,
                                Type = r?.Element(ns + "CreditRepositorySourceType")?.Value
                            })
                            ?.ToArray(),
                        CreditComments = x
                            ?.Element(ns + "CREDIT_COMMENTS")
                            ?.Elements()
                            ?.Select(r => new CreditComment
                            {
                                Code = r?.Element(ns + "CreditCommentCode")?.Value,
                                SourceType = r?.Element(ns + "CreditCommentSourceType")?.Value,
                                Text = r?.Element(ns + "CreditCommentText")?.Value,
                                Type = r?.Element(ns + "CreditCommentType")?.Value
                            })
                            ?.ToArray()
                    };
                })?.ToList();
        }

        public static List<Role> GetRoles(XElement response, XNamespace ns = null)
        {
            ns = ns ?? response.GetDefaultNamespace();
            return response
                ?.Element(ns + "PARTIES")
                ?.Elements()
                ?.SelectMany(x =>
                {
                    var d7p1 = x.GetNamespaceOfPrefix("d7p1");
                    var partySequenceNumber = x?.Attribute("SequenceNumber")?.Value;
                    var partyLabel = x?.Attribute(d7p1 + "label")?.Value;
                    var firstName = x?.NestedElement("INDIVIDUAL/NAME/FirstName")?.Value;
                    var lastName = x?.NestedElement("INDIVIDUAL/NAME/LastName")?.Value;
                    var id = x?.NestedElement("TAXPAYER_IDENTIFIERS/TAXPAYER_IDENTIFIER/TaxpayerIdentifierValue")?.Value;
                    var phone = Operations.FormatPhone(x?.NestedElement("INDIVIDUAL/CONTACT_POINTS/CONTACT_POINT/CONTACT_POINT_TELEPHONE/ContactPointTelephoneValue")?.Value);

                    return x
                        ?.Element(ns + "ROLES")
                        ?.Elements()
                        ?.Select(r => new Role
                        {
                            Identifier = id,
                            Label = r?.Attribute(d7p1 + "label")?.Value,
                            RoleName = r?.Elements()?.FirstOrDefault()?.Name?.LocalName,
                            MaritalStatus = r?.NestedElement("BORROWER/BORROWER_DETAIL/MaritalStatusType")?.Value,
                            Classification = r?.NestedElement("BORROWER/BORROWER_DETAIL/BorrowerClassificationType")?.Value,
                            BirthDate = r?.NestedElement("BORROWER/BORROWER_DETAIL/BorrowerBirthDate")?.Value,
                            FirstName = firstName,
                            LastName = lastName,
                            PartySequenceNumber = partySequenceNumber,
                            PartyLabel = partyLabel,
                            Phone = phone
                        });
                })?.ToList();
        }

    }
}
