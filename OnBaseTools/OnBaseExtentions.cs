using Hyland.Unity;
using Hyland.Unity.UnityForm;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharedResources
{
    public static class OnBaseExtentions
    {
        public static DocumentList GetDocuments(this Application app, string documentType, long max = (long)1e10)
        {
            DocumentQuery docQuery = app.Core.CreateDocumentQuery();
            docQuery.AddDocumentType(app.Core.DocumentTypes.Find(documentType));
            return docQuery.Execute(max);
        }

        public static DocumentList GetRelated(this Document document) => document.CrossReference((long)1e10, 1, 1, 1);


        public static DocumentList GetAllDocumentsInGroup(this Application app, string documentTypeGroup, DateTime? from = null, DateTime? to = null)
        {
            DocumentQuery docQuery = app.Core.CreateDocumentQuery();
            docQuery.AddDateRange(from ?? DateTime.Today.AddDays(-30), to ?? DateTime.Today.AddDays(1));
            DocumentTypeGroup dGroup = app.Core.DocumentTypeGroups.Find(documentTypeGroup);
            foreach (DocumentType dType in dGroup.DocumentTypes)
                docQuery.AddDocumentType(dType);

            return docQuery.Execute((long)(1e10));
        }

        public static DocumentList GetAllDocuments(this Application app, string documentType, DateTime? from = null, DateTime? to = null)
        {
            DocumentQuery docQuery = app.Core.CreateDocumentQuery();
            docQuery.AddDateRange(from ?? DateTime.Today.AddDays(-30), to ?? DateTime.Today.AddDays(1));
            DocumentType dType = app.Core.DocumentTypes.Find(documentType);
            docQuery.AddDocumentType(dType);

            return app.GetAllDocuments(docQuery);
        }

        public static DocumentList GetAllDocuments(this Application app, DocumentQuery docQuery) => docQuery.Execute((long)(1e10));

        public static void LogMessage(this Application app, Document document, string meessage)
        {
            app.Core.LogManagement.CreateDocumentHistoryItem(document, meessage);
        }

        public static Document CreateOnbaseFile(this Application app, byte[] data, string extention, string documentType, string fileType = null, IEnumerable<KeyValuePair<string, object>> keywords = null)
        {
            using (var stream = new MemoryStream(data))
                return app.CreateOnbaseFile(stream, extention, documentType, fileType, keywords);
        }

        public static Document CreateOnbaseFile(this Application app, Stream data, string extention, string documentType, string fileType = null, IEnumerable<KeyValuePair<string, object>> keywords = null)
        {
            DocumentType newDocType = app.Core.DocumentTypes.Find(documentType);
            if (newDocType == null)
                throw new Exception($"No Document Type exists in onbase named '{documentType}'");

            FileType newDocumentFileType;
            if (fileType == null)
            {
                var trimmedExtention = extention.TrimStart('.');
                newDocumentFileType = app.Core.FileTypes.FirstOrDefault(a => a.Extension?.Is(extention) == true || a.Extension?.Is(trimmedExtention) == true);
                if (newDocumentFileType == null)
                    throw new Exception($"No File Type found with extention {extention}");
            }
            else
            {
                newDocumentFileType = app.Core.FileTypes.Find(fileType);
                if (newDocumentFileType == null)
                    throw new Exception($"No File Type found named '{fileType}'");
            }

            var docProps = app.Core.Storage.CreateStoreNewDocumentProperties(newDocType, newDocumentFileType);
            var doc = app.Core.Storage.CreatePageData(data, extention);

            if (keywords?.Any() == true)
            {
                foreach (KeywordRecordType keywordRecordType in newDocType.KeywordRecordTypes)
                {
                    bool isStandard = keywordRecordType.RecordType != RecordType.MultiInstance; //keywordRecordType.Name == "Standard Keyword Types";
                    var newKeywordRecord = isStandard ? null : keywordRecordType.CreateEditableKeywordRecord();
                    foreach (KeywordType keywordType in keywordRecordType.KeywordTypes)
                    {
                        keywords
                        ?.Where(kw => kw.Key.Is(keywordType.Name) && kw.Value != null)
                        ?.ForEach(kw =>
                        {
                            Keyword newKeyword = null;
                            try
                            {
                                var value = kw.Value;
                                var valueType = value.GetType();

                                if (keywordType.DataType == KeywordDataType.AlphaNumeric || keywordType.DataType == KeywordDataType.Undefined)
                                    newKeyword = keywordType.CreateKeyword(kw.Value as string);
                                else if ((keywordType.DataType == KeywordDataType.Date || keywordType.DataType == KeywordDataType.DateTime))
                                {
                                    if (value is DateTime)
                                        newKeyword = keywordType.CreateKeyword((DateTime)value);
                                    else if (DateTime.TryParse(value?.ToString(), out DateTime parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }
                                else if (keywordType.DataType == KeywordDataType.Numeric9 || keywordType.DataType == KeywordDataType.Numeric20)
                                {
                                    if (value is long || value is int)
                                        newKeyword = keywordType.CreateKeyword((long)value);
                                    else if (long.TryParse(value?.ToString(), out long parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }
                                else if (keywordType.DataType == KeywordDataType.FloatingPoint)
                                {
                                    if (value is double)
                                        newKeyword = keywordType.CreateKeyword((double)value);
                                    else if (double.TryParse(value?.ToString(), out double parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }
                                else if (keywordType.DataType == KeywordDataType.Currency || keywordType.DataType == KeywordDataType.SpecificCurrency)
                                {
                                    if (value is decimal)
                                        newKeyword = keywordType.CreateKeyword((decimal)value);
                                    else if (decimal.TryParse(value?.ToString(), out decimal parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }

                                if (newKeyword != null)
                                {
                                    if (isStandard)
                                        docProps.AddKeyword(newKeyword);
                                    else
                                        newKeywordRecord.AddKeyword(newKeyword);
                                }
                            }
                            catch (Exception e)
                            {
                                e.Data.Add("keywordType", keywordType.DataType.ToString());
                                e.Data.Add("keywordName", kw.Key);
                                e.Data.Add("keywordValue", kw.Value);
                                e.Data.Add("keywordValueType", kw.Value.GetType().Name);
                                throw;
                            }
                        });
                    }
                    if (newKeywordRecord != null)
                        docProps.AddKeywordRecord(newKeywordRecord);
                }
            }
            return app.Core.Storage.StoreNewDocument(doc, docProps);
        }

        public static Document CreateOnbaseFile(this Application app, byte[] data, string extention, string documentType, JArray keywordRows, string fileType = null)
        {
            using (var stream = new MemoryStream(data))
                return app.CreateOnbaseFile(stream, extention, documentType, keywordRows, fileType);
        }

        public static Document CreateOnbaseFile(this Application app, Stream data, string extention, string documentType, JArray keywordRows, string fileType = null)
        {
            DocumentType newDocType = app.Core.DocumentTypes.Find(documentType);
            if (newDocType == null)
                throw new Exception($"No Document Type found named '{documentType}'");

            FileType newDocumentFileType;
            if (fileType == null)
            {
                var trimmedExtention = extention.TrimStart('.');
                newDocumentFileType = app.Core.FileTypes.FirstOrDefault(a => a.Extension?.Is(extention) == true || a.Extension?.Is(trimmedExtention) == true);
                if (newDocumentFileType == null)
                    throw new Exception($"No File Type found with extention {extention}");
            }
            else
            {
                newDocumentFileType = app.Core.FileTypes.Find(fileType);
                if (newDocumentFileType == null)
                    throw new Exception($"No File Type found named '{fileType}'");
            }

            var docProps = app.Core.Storage.CreateStoreNewDocumentProperties(newDocType, newDocumentFileType);
            var doc = app.Core.Storage.CreatePageData(data, extention);
            foreach (JObject keywordRow in keywordRows)
            {
                foreach (KeywordRecordType keywordRecordType in newDocType.KeywordRecordTypes)
                {
                    bool additionRequired = false;
                    bool isStandard = keywordRecordType.RecordType != RecordType.MultiInstance; //keywordRecordType.Name == "Standard Keyword Types";
                    var newKeywordRecord = isStandard ? null : keywordRecordType.CreateEditableKeywordRecord();

                    foreach (KeywordType keywordType in keywordRecordType.KeywordTypes)
                    {
                        keywordRow
                        ?.ToObject<Dictionary<string, object>>()
                        ?.Where(keyword => keyword.Key.Is(keywordType.Name) && keyword.Value != null)
                        ?.ForEach(keyword =>
                        {
                            Keyword newKeyword = null;
                            try
                            {
                                var value = keyword.Value;
                                if (keywordType.DataType == KeywordDataType.AlphaNumeric || keywordType.DataType == KeywordDataType.Undefined)
                                {
                                    var strValue = value?.ToString();
                                    if (!string.IsNullOrWhiteSpace(strValue))
                                        newKeyword = keywordType.CreateKeyword(strValue);
                                }
                                else if ((keywordType.DataType == KeywordDataType.Date || keywordType.DataType == KeywordDataType.DateTime))
                                {
                                    if (value is DateTime)
                                        newKeyword = keywordType.CreateKeyword((DateTime)value);
                                    else if (DateTime.TryParse(value?.ToString(), out DateTime parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }
                                else if (keywordType.DataType == KeywordDataType.Numeric9 || keywordType.DataType == KeywordDataType.Numeric20)
                                {
                                    if (value is long || value is int)
                                        newKeyword = keywordType.CreateKeyword((long)value);
                                    else if (long.TryParse(value?.ToString(), out long parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }
                                else if (keywordType.DataType == KeywordDataType.FloatingPoint)
                                {
                                    if (value is double)
                                        newKeyword = keywordType.CreateKeyword((double)value);
                                    else if (double.TryParse(value?.ToString(), out double parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }
                                else if (keywordType.DataType == KeywordDataType.Currency || keywordType.DataType == KeywordDataType.SpecificCurrency)
                                {
                                    if (value is decimal)
                                        newKeyword = keywordType.CreateKeyword((decimal)value);
                                    else if (decimal.TryParse(value?.ToString(), out decimal parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }

                                if (newKeyword != null)
                                {
                                    if (isStandard)
                                        docProps.AddKeyword(newKeyword);
                                    else
                                    {
                                        newKeywordRecord.AddKeyword(newKeyword);
                                        additionRequired = true;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                e.Data.Add("keywordType", keywordType.DataType.ToString());
                                e.Data.Add("keywordName", keyword.Key);
                                e.Data.Add("keywordValue", keyword.Value);
                                e.Data.Add("keywordValueType", keyword.Value.GetType().Name);
                                throw;
                            }
                        });
                    }
                    if (newKeywordRecord != null && additionRequired)
                        docProps.AddKeywordRecord(newKeywordRecord);
                }
            }
            return app.Core.Storage.StoreNewDocument(doc, docProps);
        }

        public static Document CreateOnbaseForm(this Application app, string templateName, JArray keywordRows, IEnumerable<string> autofillKeywordSets = null)
        {
            var newForm = app.Core.UnityFormTemplates.Find(templateName);

            if (newForm == null)
                throw new Exception($"No Form Template found named '{templateName}'");

            DocumentType newDocType = newForm.DocumentType;
            var docProps = app.Core.Storage.CreateStoreNewUnityFormProperties(newForm);

            //To determine if the repeating keywords contain an auto increasing number
            KeywordType numberKeyword = app.Core.KeywordTypes.Find("#");
            var numberTrackers = new Dictionary<string, int>();

            var autofillSets = autofillKeywordSets
                ?.Select(a =>
                {
                    var set = app
                        ?.Core
                        ?.AutoFillKeywordSets
                        ?.Find(a);
                    if (set == null)
                        return null;
                    return new { key = set.PrimaryKeywordType.Name, value = set };
                })
                ?.Where(a => a != null)
                ?.ToList();

            foreach (JObject keywordRow in keywordRows)
            {
                List<EditableKeywordRecord> newKeywordRecords = new List<EditableKeywordRecord>();
                foreach (KeyValuePair<string, object> keyword in keywordRow?.ToObject<Dictionary<string, object>>()?.Where(kw => kw.Value != null))
                {
                    KeywordType keywordType = app.Core.KeywordTypes.Find(keyword.Key);
                    if (keywordType != null)
                    {
                        KeywordRecordType keywordRecordType = newDocType.KeywordRecordTypes.Find(a => a.KeywordTypes.Contains(keywordType));
                        if (keywordRecordType != null)
                        {
                            bool isStandard = keywordRecordType.RecordType != RecordType.MultiInstance; //keywordRecordType.Name == "Standard Keyword Types";
                            Keyword newKeyword = null;
                            try
                            {
                                var value = keyword.Value;
                                if (keywordType.DataType == KeywordDataType.AlphaNumeric || keywordType.DataType == KeywordDataType.Undefined)
                                {
                                    var strValue = value?.ToString();
                                    if (!string.IsNullOrWhiteSpace(strValue))
                                        newKeyword = keywordType.CreateKeyword(strValue);
                                }
                                else if ((keywordType.DataType == KeywordDataType.Date || keywordType.DataType == KeywordDataType.DateTime))
                                {
                                    if (value is DateTime)
                                        newKeyword = keywordType.CreateKeyword((DateTime)value);
                                    else if (DateTime.TryParse(value?.ToString(), out DateTime parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }
                                else if (keywordType.DataType == KeywordDataType.Numeric9 || keywordType.DataType == KeywordDataType.Numeric20)
                                {
                                    if (value is long || value is int)
                                        newKeyword = keywordType.CreateKeyword((long)value);
                                    else if (long.TryParse(value?.ToString(), out long parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }
                                else if (keywordType.DataType == KeywordDataType.FloatingPoint)
                                {
                                    if (value is double)
                                        newKeyword = keywordType.CreateKeyword((double)value);
                                    else if (double.TryParse(value?.ToString(), out double parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }
                                else if (keywordType.DataType == KeywordDataType.Currency || keywordType.DataType == KeywordDataType.SpecificCurrency)
                                {
                                    if (value is decimal)
                                        newKeyword = keywordType.CreateKeyword((decimal)value);
                                    else if (decimal.TryParse(value?.ToString(), out decimal parsedValue))
                                        newKeyword = keywordType.CreateKeyword(parsedValue);
                                }

                                if (newKeyword != null)
                                {
                                    if (isStandard)
                                        docProps.AddKeyword(newKeyword);
                                    else
                                    {
                                        EditableKeywordRecord newKeywordRecord = newKeywordRecords.FirstOrDefault(a => a.KeywordRecordType == keywordRecordType);
                                        if (newKeywordRecord == null)
                                        {
                                            newKeywordRecord = keywordRecordType.CreateEditableKeywordRecord();
                                            if (keywordRecordType.KeywordTypes.Contains(numberKeyword))
                                            {
                                                if (numberTrackers.ContainsKey(keywordRecordType.Name))
                                                    numberTrackers[keywordRecordType.Name]++;
                                                else
                                                    numberTrackers.Add(keywordRecordType.Name, 1);

                                                newKeywordRecord.AddKeyword("#", numberTrackers[keywordRecordType.Name]);
                                            }
                                            newKeywordRecords.Add(newKeywordRecord);
                                        }
                                        newKeywordRecord.AddKeyword(newKeyword);

                                        autofillSets
                                            ?.Where(autofillSet => autofillSet?.key == keywordType.Name)
                                            ?.ForEach(autofillSet =>
                                            {
                                                autofillSet
                                                    ?.value
                                                    ?.GetKeysetData(newKeyword)
                                                    ?.ForEach(keysetData =>
                                                    {
                                                        keysetData
                                                            ?.Keywords
                                                            ?.Where(b => b.KeywordType.Name != autofillSet.key)
                                                            ?.ForEach(b => { newKeywordRecord.AddKeyword(b); });
                                                    });
                                            });

                                        //if (autofillKeywordSets?.Contains(keyword.Key, StringComparer.InvariantCultureIgnoreCase) == true)
                                        //{
                                        //    app
                                        //        ?.Core
                                        //        ?.AutoFillKeywordSets
                                        //        ?.Where(a => a.PrimaryKeywordType == keywordType)
                                        //        ?.ForEach(fillSet =>
                                        //        {
                                        //            fillSet
                                        //             ?.GetKeysetData(newKeyword)
                                        //             ?.ForEach(set =>
                                        //             {
                                        //                 set
                                        //                     ?.Keywords
                                        //                     ?.Where(a => a.KeywordType != set.PrimaryKeyword.KeywordType)
                                        //                     ?.ForEach(a => { newKeywordRecord.AddKeyword(a); });
                                        //             });
                                        //        });
                                        //}
                                    }
                                }
                                else if (!string.IsNullOrWhiteSpace(value?.ToString()))
                                {
                                    var e = new Exception("Indeterminable Keyword Value");
                                    e.Data.Add("Type", keywordType.DataType.ToString());
                                    e.Data.Add("Name", keyword.Key);
                                    e.Data.Add("Value", value);
                                    e.Data.Add("ValueType", keyword.Value.GetType().Name);
                                    throw e;
                                }
                            }
                            catch (Exception e)
                            {
                                e.Data.Add("keywordType", keywordType.DataType.ToString());
                                e.Data.Add("keywordName", keyword.Key);
                                e.Data.Add("keywordValue", keyword.Value);
                                e.Data.Add("keywordValueType", keyword.Value.GetType().Name);
                                throw;
                            }
                        }
                    }
                    else
                    {
                        ValueFieldDefinition fieldDefinition = newForm.AllFieldDefinitions.ValueFieldDefinitions.Find(keyword.Key);
                        if (fieldDefinition != null && fieldDefinition.KeywordType == null)
                        {
                            Hyland.Unity.UnityForm.Field newField = null;
                            var value = keyword.Value;
                            if (fieldDefinition.DataType == FieldDataType.AlphaNumeric)
                            {
                                var strValue = value?.ToString();
                                if (!string.IsNullOrWhiteSpace(strValue))
                                    newField = fieldDefinition.CreateField(strValue);
                            }
                            else if ((fieldDefinition.DataType == FieldDataType.Date || fieldDefinition.DataType == FieldDataType.DateTime))
                            {
                                if (value is DateTime)
                                    newField = fieldDefinition.CreateField((DateTime)value);
                                else if (DateTime.TryParse(value?.ToString(), out DateTime parsedValue))
                                    newField = fieldDefinition.CreateField(parsedValue);
                            }
                            else if (fieldDefinition.DataType == FieldDataType.Numeric9 || fieldDefinition.DataType == FieldDataType.Numeric20)
                            {
                                if (value is long || value is int)
                                    newField = fieldDefinition.CreateField((long)value);
                                else if (long.TryParse(value?.ToString(), out long parsedValue))
                                    newField = fieldDefinition.CreateField(parsedValue);
                            }
                            else if (fieldDefinition.DataType == FieldDataType.FloatingPoint)
                            {
                                if (value is double)
                                    newField = fieldDefinition.CreateField((double)value);
                                else if (double.TryParse(value?.ToString(), out double parsedValue))
                                    newField = fieldDefinition.CreateField(parsedValue);
                            }
                            else if (fieldDefinition.DataType == FieldDataType.Currency || fieldDefinition.DataType == FieldDataType.Decimal)
                            {
                                if (value is decimal)
                                    newField = fieldDefinition.CreateField((decimal)value);
                                else if (decimal.TryParse(value?.ToString(), out decimal parsedValue))
                                    newField = fieldDefinition.CreateField(parsedValue);
                            }
                            else if (fieldDefinition.DataType == FieldDataType.Boolean)
                            {
                                if (value is bool)
                                    newField = fieldDefinition.CreateField((bool)value);
                                else if (bool.TryParse(value?.ToString(), out bool parsedValue))
                                    newField = fieldDefinition.CreateField(parsedValue);
                            }

                            if (newField != null)
                                docProps.AddField(newField);
                            else if (!string.IsNullOrWhiteSpace(value?.ToString()))
                            {
                                var e = new Exception("Indeterminable Field Value");
                                e.Data.Add("Type", fieldDefinition.DataType.ToString());
                                e.Data.Add("Name", keyword.Key);
                                e.Data.Add("Value", value);
                                e.Data.Add("ValueType", keyword.Value.GetType().Name);
                                throw e;
                            }
                        }
                    }
                }
                newKeywordRecords.ForEach(a => docProps.AddKeywordRecord(a));
            }
            return app.Core.Storage.StoreNewUnityForm(docProps);
        }

        public static object GetFieldValue(this KeywordType keywordType, Document document)
        {
            return document.KeywordRecords.Find(keywordType).Keywords.Find(keywordType.Name)?.Value;
        }

        public static JObject GetFieldValues(this Document document, params string[] fields)
        {
            var jObj = new JObject();
            foreach (KeywordRecord keywordRecord in document.KeywordRecords)
            {
                foreach (Keyword keyword in keywordRecord.Keywords)
                {
                    if (fields.Contains(keyword.KeywordType.Name))
                    {
                        if (!jObj.ContainsKey(keyword.KeywordType.Name))
                            jObj[keyword.KeywordType.Name] = new JArray();
                        (jObj[keyword.KeywordType.Name] as JArray).Add(keyword.Value);
                    }
                }
            }

            return jObj;
        }

        public static JObject GetKeywordRecords(this Document document)
        {
            var jObj = new JObject();
            foreach (KeywordRecord keywordRecord in document.KeywordRecords)
            {
                var record = new JObject();

                foreach (Keyword keyword in keywordRecord.Keywords)
                {
                    var key = keyword.KeywordType.Name;
                    if (!record.ContainsKey(key))
                        record[key] = new JValue(keyword.Value);
                    else if (record[key].Type == JTokenType.Array)
                        (record[key] as JArray).Add(new JValue(keyword.Value));
                    else
                    {
                        JToken existing = record[key];
                        record[key] = new JArray {
                            existing,
                            new JValue(keyword.Value)
                        };
                    }
                }

                if (!jObj.ContainsKey(keywordRecord.KeywordRecordType.Name))
                    jObj[keywordRecord.KeywordRecordType.Name] = new JArray();
                (jObj[keywordRecord.KeywordRecordType.Name] as JArray).Add(record);
            }
            return jObj;
        }

        public static JObject GetFieldValues(this Form form)
        {
            var jObj = new JObject();
            form?.AllFields?.ValueFields?.ForEach(item => jObj[item.FieldDefinition.Name] = item.IsEmpty ? "" : JToken.FromObject(item.Value));
            form?.AllFields?.RepeaterFields?.ForEach(item => jObj[item.FieldDefinition.Name] = GetRepeaterArray(item));
            form?.AllFields?.NestedTableFields?.ForEach(item => jObj[item.FieldDefinition.Name] = GetTableArray(item));
            return jObj;
        }

        public static JArray GetRepeaterArray(this Repeater repeater)
        {
            var jArr = new JArray();
            repeater?.RepeaterItems?.ForEach(item => jArr.Add(GetRepeaterItem(item)));
            return jArr;
        }

        public static JArray GetTableArray(this NestedTable nestedTable)
        {
            var jArr = new JArray();
            nestedTable?.NestedTableItems?.ForEach(item => jArr.Add(GetRepeaterItem(item)));
            return jArr;
        }

        public static JObject GetRepeaterItem<T>(this RepeaterItemBase<T> repeaterItem) where T : RepeaterDefinitionBase
        {
            var jObj = new JObject();
            repeaterItem?.AllFields?.ValueFields?.ForEach(item => jObj[item.FieldDefinition.Name] = item.IsEmpty ? "" : JToken.FromObject(item.Value));
            repeaterItem?.AllFields?.RepeaterFields?.ForEach(item => jObj[item.FieldDefinition.Name] = GetRepeaterArray(item));
            repeaterItem?.AllFields?.NestedTableFields?.ForEach(item => jObj[item.FieldDefinition.Name] = GetTableArray(item));
            return jObj;
        }
    }
}
